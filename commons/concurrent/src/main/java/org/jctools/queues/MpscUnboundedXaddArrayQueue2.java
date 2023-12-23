/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package org.jctools.queues;

import cn.wjybxx.common.annotation.Internal;
import org.jctools.util.PortableJvmInfo;

/**
 * 修改自{@link MpscUnboundedXaddArrayQueue}，提供了一些框架用的特殊接口；用户不要直接使用该类
 * ps:放在同一个包下以访问一些特殊的接口，这就是Java漏洞。
 *
 * @author wjybxx
 * date - 2023/9/15
 */
@Internal
public final class MpscUnboundedXaddArrayQueue2<E> extends MpUnboundedXaddArrayQueue<MpscUnboundedXaddChunk<E>, E> {

    /**
     * @param chunkSize       每个块的大小
     * @param maxPooledChunks 块的缓存数量
     */
    public MpscUnboundedXaddArrayQueue2(int chunkSize, int maxPooledChunks) {
        super(chunkSize, maxPooledChunks);
    }

    public MpscUnboundedXaddArrayQueue2(int chunkSize) {
        super(chunkSize, 2);
    }

    // region 自定义逻辑

    public interface OfferHooker<E> {

        /** @return 不可返回null */
        E translate(E event, long sequence);

        void hook(E srcEvent, E destEvent, long sequence);
    }

    /** 使用该方法发布事件可避免逆向查询Chunk */
    public void offerX(final E src, OfferHooker<E> translator) {
        if (null == src) {
            throw new NullPointerException();
        }
        final int chunkMask = this.chunkMask;
        final int chunkShift = this.chunkShift;

        final long pIndex = getAndIncrementProducerIndex();
        E dest = translator.translate(src, pIndex); // 外部填充数据
        if (dest == null) {
            throw new Error("translate event to null");
        }

        final int piChunkOffset = (int) (pIndex & chunkMask);
        final long piChunkIndex = pIndex >> chunkShift;

        MpscUnboundedXaddChunk<E> pChunk = lvProducerChunk();
        if (pChunk.lvIndex() != piChunkIndex) {
            // Other producers may have advanced the producer chunk as we claimed a slot in a prev chunk, or we may have
            // now stepped into a brand new chunk which needs appending.
            pChunk = producerChunkForIndex(pChunk, piChunkIndex);
        }
        pChunk.soElement(piChunkOffset, dest);

        translator.hook(src, dest, pIndex);
    }

    public long next() {
        long pIndex = getAndIncrementProducerIndex();

        // 确保新块添加
        final long piChunkIndex = pIndex >> chunkShift;
        final MpscUnboundedXaddChunk<E> pChunk = lvProducerChunk();
        if (pChunk.lvIndex() != piChunkIndex) {
            producerChunkForIndex(pChunk, piChunkIndex);
        }

        return pIndex;
    }

    public void publish(long sequence, E e) {
        if (null == e) {
            throw new NullPointerException();
        }
        MpscUnboundedXaddChunk<E> pChunk = findProducerChunk(sequence);
        assert pChunk != null : sequence;

        final int chunkMask = this.chunkMask;
        final int piChunkOffset = (int) (sequence & chunkMask);
        pChunk.soElement(piChunkOffset, e);
    }

    private MpscUnboundedXaddChunk<E> findProducerChunk(long sequence) {
        final long requiredChunkIndex = sequence >> chunkShift;

        MpscUnboundedXaddChunk<E> currentChunk = lvProducerChunk();
        long currentChunkIndex = currentChunk.lvIndex();

        // 竞争不那么激烈的情况下，几乎总会命中当前块；因为申请序号后的逻辑只是填充数据而已，没有耗时逻辑
        if (currentChunkIndex == requiredChunkIndex) {
            return currentChunk;
        }

        long expectedChunkIndex = currentChunkIndex - 1;
        while (true) {
            currentChunk = currentChunk.lvPrev();
            if (currentChunk == null) { // 前一个块已被消费
                return null;
            }
            currentChunkIndex = currentChunk.lvIndex();
            if (currentChunkIndex != expectedChunkIndex) { // 前一个块已被消费
                return null;
            }
            if (currentChunkIndex == requiredChunkIndex) { // 目标块
                return currentChunk;
            }
            expectedChunkIndex--;
        }
    }

    // endregion

    // region 未改动代码

    @Override
    final MpscUnboundedXaddChunk<E> newChunk(long index, MpscUnboundedXaddChunk<E> prev, int chunkSize, boolean pooled) {
        return new MpscUnboundedXaddChunk<>(index, prev, chunkSize, pooled);
    }

    @Override
    public boolean offer(E e) {
        if (null == e) {
            throw new NullPointerException();
        }
        final int chunkMask = this.chunkMask;
        final int chunkShift = this.chunkShift;

        final long pIndex = getAndIncrementProducerIndex();

        final int piChunkOffset = (int) (pIndex & chunkMask);
        final long piChunkIndex = pIndex >> chunkShift;

        MpscUnboundedXaddChunk<E> pChunk = lvProducerChunk();
        if (pChunk.lvIndex() != piChunkIndex) {
            // Other producers may have advanced the producer chunk as we claimed a slot in a prev chunk, or we may have
            // now stepped into a brand new chunk which needs appending.
            pChunk = producerChunkForIndex(pChunk, piChunkIndex);
        }
        pChunk.soElement(piChunkOffset, e);
        return true;
    }

    private MpscUnboundedXaddChunk<E> pollNextBuffer(MpscUnboundedXaddChunk<E> cChunk, long cIndex) {
        final MpscUnboundedXaddChunk<E> next = spinForNextIfNotEmpty(cChunk, cIndex);

        if (next == null) {
            return null;
        }

        moveToNextConsumerChunk(cChunk, next);
        assert next.lvIndex() == cIndex >> chunkShift;
        return next;
    }

    private MpscUnboundedXaddChunk<E> spinForNextIfNotEmpty(MpscUnboundedXaddChunk<E> cChunk, long cIndex) {
        MpscUnboundedXaddChunk<E> next = cChunk.lvNext();
        if (next == null) {
            if (lvProducerIndex() == cIndex) {
                return null;
            }
            final long ccChunkIndex = cChunk.lvIndex();
            if (lvProducerChunkIndex() == ccChunkIndex) {
                // no need to help too much here or the consumer latency will be hurt
                next = appendNextChunks(cChunk, ccChunkIndex, 1);
            }
            while (next == null) {
                next = cChunk.lvNext();
            }
        }
        return next;
    }

    @Override
    public E poll() {
        final int chunkMask = this.chunkMask;
        final long cIndex = this.lpConsumerIndex();
        final int ciChunkOffset = (int) (cIndex & chunkMask);

        MpscUnboundedXaddChunk<E> cChunk = this.lvConsumerChunk();
        // start of new chunk?
        if (ciChunkOffset == 0 && cIndex != 0) {
            // pollNextBuffer will verify emptiness check
            cChunk = pollNextBuffer(cChunk, cIndex);
            if (cChunk == null) {
                return null;
            }
        }

        E e = cChunk.lvElement(ciChunkOffset);
        if (e == null) {
            if (lvProducerIndex() == cIndex) {
                return null;
            } else {
                e = cChunk.spinForElement(ciChunkOffset, false);
            }
        }
        cChunk.soElement(ciChunkOffset, null);
        soConsumerIndex(cIndex + 1);
        return e;
    }

    @Override
    public E peek() {
        final int chunkMask = this.chunkMask;
        final long cIndex = this.lpConsumerIndex();
        final int ciChunkOffset = (int) (cIndex & chunkMask);

        MpscUnboundedXaddChunk<E> cChunk = this.lpConsumerChunk();
        // start of new chunk?
        if (ciChunkOffset == 0 && cIndex != 0) {
            cChunk = spinForNextIfNotEmpty(cChunk, cIndex);
            if (cChunk == null) {
                return null;
            }
        }

        E e = cChunk.lvElement(ciChunkOffset);
        if (e == null) {
            if (lvProducerIndex() == cIndex) {
                return null;
            } else {
                e = cChunk.spinForElement(ciChunkOffset, false);
            }
        }
        return e;
    }

    @Override
    public E relaxedPoll() {
        final int chunkMask = this.chunkMask;
        final long cIndex = this.lpConsumerIndex();
        final int ciChunkOffset = (int) (cIndex & chunkMask);

        MpscUnboundedXaddChunk<E> cChunk = this.lpConsumerChunk();
        E e;
        // start of new chunk?
        if (ciChunkOffset == 0 && cIndex != 0) {
            final MpscUnboundedXaddChunk<E> next = cChunk.lvNext();
            if (next == null) {
                return null;
            }
            e = next.lvElement(0);

            // if the next chunk doesn't have the first element set we give up
            if (e == null) {
                return null;
            }
            moveToNextConsumerChunk(cChunk, next);

            cChunk = next;
        } else {
            e = cChunk.lvElement(ciChunkOffset);
            if (e == null) {
                return null;
            }
        }

        cChunk.soElement(ciChunkOffset, null);
        soConsumerIndex(cIndex + 1);
        return e;
    }

    @Override
    public E relaxedPeek() {
        final int chunkMask = this.chunkMask;
        final long cIndex = this.lpConsumerIndex();
        final int cChunkOffset = (int) (cIndex & chunkMask);

        MpscUnboundedXaddChunk<E> cChunk = this.lpConsumerChunk();

        // start of new chunk?
        if (cChunkOffset == 0 && cIndex != 0) {
            cChunk = cChunk.lvNext();
            if (cChunk == null) {
                return null;
            }
        }
        return cChunk.lvElement(cChunkOffset);
    }

    @Override
    public int fill(Supplier<E> s) {
        long result = 0;// result is a long because we want to have a safepoint check at regular intervals
        final int capacity = chunkMask + 1;
        final int offerBatch = Math.min(PortableJvmInfo.RECOMENDED_OFFER_BATCH, capacity);
        do {
            final int filled = fill(s, offerBatch);
            if (filled == 0) {
                return (int) result;
            }
            result += filled;
        }
        while (result <= capacity);
        return (int) result;
    }

    @Override
    public int drain(Consumer<E> c, int limit) {
        if (null == c)
            throw new IllegalArgumentException("c is null");
        if (limit < 0)
            throw new IllegalArgumentException("limit is negative: " + limit);
        if (limit == 0)
            return 0;

        final int chunkMask = this.chunkMask;

        long cIndex = this.lpConsumerIndex();

        MpscUnboundedXaddChunk<E> cChunk = this.lpConsumerChunk();

        for (int i = 0; i < limit; i++) {
            final int consumerOffset = (int) (cIndex & chunkMask);
            E e;
            if (consumerOffset == 0 && cIndex != 0) {
                final MpscUnboundedXaddChunk<E> next = cChunk.lvNext();
                if (next == null) {
                    return i;
                }
                e = next.lvElement(0);

                // if the next chunk doesn't have the first element set we give up
                if (e == null) {
                    return i;
                }
                moveToNextConsumerChunk(cChunk, next);

                cChunk = next;
            } else {
                e = cChunk.lvElement(consumerOffset);
                if (e == null) {
                    return i;
                }
            }
            cChunk.soElement(consumerOffset, null);
            final long nextConsumerIndex = cIndex + 1;
            soConsumerIndex(nextConsumerIndex);
            c.accept(e);
            cIndex = nextConsumerIndex;
        }
        return limit;
    }

    @Override
    public int fill(Supplier<E> s, int limit) {
        if (null == s)
            throw new IllegalArgumentException("supplier is null");
        if (limit < 0)
            throw new IllegalArgumentException("limit is negative:" + limit);
        if (limit == 0)
            return 0;

        final int chunkShift = this.chunkShift;
        final int chunkMask = this.chunkMask;

        long pIndex = getAndAddProducerIndex(limit);
        MpscUnboundedXaddChunk<E> pChunk = null;
        for (int i = 0; i < limit; i++) {
            final int pChunkOffset = (int) (pIndex & chunkMask);
            final long chunkIndex = pIndex >> chunkShift;
            if (pChunk == null || pChunk.lvIndex() != chunkIndex) {
                pChunk = producerChunkForIndex(pChunk, chunkIndex);
            }
            pChunk.soElement(pChunkOffset, s.get());
            pIndex++;
        }
        return limit;
    }

    // endregion

}