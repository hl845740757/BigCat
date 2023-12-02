package cn.wjybxx.common.collect;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.Preconditions;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Comparator;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.function.ObjIntConsumer;

/**
 * @author wjybxx
 * date 2023/4/6
 */
@NotThreadSafe
public final class DelayedCompressListImpl<E> implements DelayedCompressList<E> {

    private final ArrayList<E> children;
    private int recursionDepth;

    /** 记录删除的元素的范围，避免迭代所有 */
    private transient int firstIndex = INDEX_NOT_FOUND;
    private transient int lastIndex = INDEX_NOT_FOUND;

    public DelayedCompressListImpl() {
        this(4);
    }

    public DelayedCompressListImpl(int initCapacity) {
        children = new ArrayList<>(initCapacity);
    }

    public DelayedCompressListImpl(Collection<? extends E> src) {
        Preconditions.checkNullElements(src);
        children = new ArrayList<>(src);
    }

    @Override
    public void beginItr() {
        recursionDepth++;
    }

    @Override
    public void endItr() {
        if (recursionDepth == 0) {
            throw new IllegalStateException("begin must be called before end.");
        }
        recursionDepth--;
        if (recursionDepth == 0 && firstIndex != INDEX_NOT_FOUND) {
            ArrayList<E> children = this.children;
            int removed = lastIndex - firstIndex + 1;
            if (removed == 1) {
                // 很少在迭代期间删除多个元素，因此我们测试是否删除了单个
                children.remove(firstIndex);
            } else if (removed == children.size()) {
                // 调用了clear
                children.clear();
            } else if (children.size() - removed <= 8) {
                // subList与源集合相近，使用subList意义不大
                children.removeIf(Objects::isNull);
            } else {
                children.subList(firstIndex, lastIndex + 1).removeIf(Objects::isNull);
            }
            firstIndex = INDEX_NOT_FOUND;
            lastIndex = INDEX_NOT_FOUND;
        }
    }

    @Override
    public boolean isIterating() {
        return recursionDepth > 0;
    }

    @Override
    public boolean isDelayed() {
        return firstIndex != INDEX_NOT_FOUND;
    }

    @Override
    public boolean add(E e) {
        Objects.requireNonNull(e);
        children.add(e);
        return true;
    }

    @Override
    public boolean addAll(@Nonnull Collection<? extends E> c) {
        Preconditions.checkNullElements(c);
        return children.addAll(c);
    }

    @Nullable
    @Override
    public E get(int index) {
        return children.get(index);
    }

    @Override
    public E set(int index, E e) {
        Objects.requireNonNull(e);
        return children.set(index, e);
    }

    @Override
    public E removeAt(int index) {
        ArrayList<E> children = this.children;
        if (children.size() == 0) {
            return null;
        }
        if (recursionDepth == 0) {
            return children.remove(index);
        }

        E removed = children.set(index, null);
        if (removed != null) {
            if (firstIndex == INDEX_NOT_FOUND || index < firstIndex) {
                firstIndex = index;
            }
            if (lastIndex == INDEX_NOT_FOUND || index > lastIndex) {
                lastIndex = index;
            }
        }
        return removed;
    }

    @Override
    public void clear() {
        ArrayList<E> children = this.children;
        if (children.size() == 0) {
            return;
        }
        if (recursionDepth == 0) {
            children.clear();
            return;
        }

        firstIndex = 0;
        lastIndex = children.size() - 1;
        children.replaceAll(e -> null); // 这个似乎更快
    }

    @Override
    public int index(@Nullable Object e) {
        if (e == null) {
            return firstIndex;
        }
        //noinspection SuspiciousMethodCalls
        return children.indexOf(e);
    }

    @Override
    public int lastIndex(@Nullable Object e) {
        if (e == null) {
            return lastIndex;
        }
        //noinspection SuspiciousMethodCalls
        return children.lastIndexOf(e);
    }

    @Override
    public int indexOfRef(@Nullable Object e) {
        if (e == null) {
            return firstIndex;
        }
        return CollectionUtils.indexOfRef(children, e);
    }

    @Override
    public int lastIndexOfRef(@Nullable Object e) {
        if (e == null) {
            return lastIndex;
        }
        return CollectionUtils.lastIndexOfRef(children, e);
    }

    @Override
    public void sort(@Nonnull Comparator<? super E> comparator) {
        Objects.requireNonNull(comparator);
        ensureNotIterating();
        children.sort(comparator);
    }

    @Override
    public int size() {
        return children.size();
    }

    @Override
    public boolean isEmpty() {
        return children.isEmpty();
    }

    @Override
    public int realSize() {
        final ArrayList<E> children = this.children;
        if (recursionDepth == 0 || firstIndex == INDEX_NOT_FOUND) { // 没有删除元素
            return children.size();
        }

        int removed = lastIndex - firstIndex + 1;
        if (removed == 1) {
            return children.size() - 1; // 删除了一个元素
        }
        if (removed == children.size()) { // 执行了clear
            return 0;
        }
        // 统计区间内非null元素
        for (int index = firstIndex, endIndex = lastIndex; index <= endIndex; index++) {
            if (children.get(index) != null) {
                removed--;
            }
        }
        return children.size() - removed;
    }

    @Override
    public boolean isRealEmpty() {
        return children.isEmpty() || firstIndex == INDEX_NOT_FOUND;
    }

    @Override
    public void forEach(Consumer<? super E> action) {
        Objects.requireNonNull(action);
        int size = size();
        if (size == 0) {
            return;
        }
        ArrayList<E> children = this.children;
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                final E e = children.get(index);
                if (e != null) {
                    action.accept(e);
                }
            }
        } finally {
            endItr();
        }
    }

    @Override
    public void forEach(ObjIntConsumer<? super E> action) {
        Objects.requireNonNull(action);
        int size = size();
        if (size == 0) {
            return;
        }
        ArrayList<E> children = this.children;
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                final E e = children.get(index);
                if (e != null) {
                    action.accept(e, index);
                }
            }
        } finally {
            endItr();
        }
    }

    //

    private void ensureNotIterating() {
        if (recursionDepth > 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

}