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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.dson.text.*;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonTokenTest {

    @Test
    void test() {
        String x = """
                -- pos: {@Vector3 x: 0.5, y: 0.5, z: 0.5}
                -- posArray: [@{clsName:LinkedList,compClsName:Vector3} {x: 0.1, y: 0.1, z: 0.1}, {x: 0.2, y: 0.2, z: 0.2}]
                --
                --
                -- [@bin 1, FFFA]
                -- [@ei 1, 10010]
                -- [@eL 1, 10010]
                -- [@es 1, 10010]
                -- @ss intro:
                ->   salkjlxaaslkhalkhsal,anxksjah\\n
                -| xalsjalkjlkalhjalskhalhslahlsanlkanclxa
                -| salkhaslkanlnlkhsjlanx,nalkxanla
                -> lsaljsaljsalsaajsal
                -> saklhskalhlsajlxlsamlkjalj
                -> salkhjsaljsljldjaslna
                --""";

        List<DsonToken> tokenList1 = new ArrayList<>(64);
        List<DsonToken> tokenList2 = new ArrayList<>(64);
        pullToList(new DsonScanner(new DsonLinesBuffer(x.lines().collect(Collectors.toList()))), tokenList1);
        pullToList(new DsonScanner(new DsonStringBuffer(x)), tokenList2);
        Assertions.assertEquals(tokenList1, tokenList2);
    }

    private static void pullToList(DsonScanner scanner, List<DsonToken> outList) {
        while (true) {
            DsonToken nextToken = scanner.nextToken();
            if (nextToken.getType() == TokenType.EOF) {
                break;
            }
            outList.add(nextToken);
        }
    }
}