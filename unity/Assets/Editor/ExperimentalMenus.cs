#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Editor
{
/// <summary>
/// 验证想法的菜单项
/// </summary>
public static class ExperimentalMenus
{
    /// <summary>
    /// 理论上越早收录进Unicode的字符，越是常用字符，而其码点通常也越小？？？
    /// 实测发现好像不是...更像是按照字典中的偏旁部首序录入到Unicode字符集的。
    /// </summary>
    // [MenuItem("Editor/SortHan7000")]
    public static void SortHan7000() {
        string text = File.ReadAllText("Assets/Editor/Resources/7000+symbols.txt");
        string sortedText = SortByCodePoint(text);
        Debug.Log(sortedText);
    }

    // [MenuItem("Editor/SortHan3000")]
    public static void SortHan3000() {
        string text = File.ReadAllText("Assets/Editor/Resources/3500+symbols.txt");
        string sortedText = SortByCodePoint(text);
        Debug.Log(sortedText);
    }

    private static string SortByCodePoint(string text) {
        List<int> codePointArray = new List<int>(text.Length);
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            int unicode;
            if (char.IsSurrogate(c)) {
                unicode = char.ConvertToUtf32(c, text[++i]);
            } else {
                unicode = c;
            }
            codePointArray.Add(unicode);
        }
        // 按unicode排序 -- 再转换字符串
        codePointArray.Sort();
        StringBuilder sb = new StringBuilder(text.Length);
        foreach (int codePoint in codePointArray) {
            if (codePoint < 65536) {
                sb.Append((char)codePoint);
            } else {
                sb.Append(char.ConvertFromUtf32(codePoint));
            }
        }
        string sortedText = sb.ToString();
        return sortedText;
    }

    /// <summary>
    /// 创建轴心在左下角的Quad
    /// </summary>
    // [MenuItem("Create/TileQuad")]
    private static void CreateTileQuad() {
        GameObject quadObj = new GameObject("TileQuad");
        MeshFilter meshFilter = quadObj.AddComponent<MeshFilter>();
        Mesh newMesh = CreateMeshWithNewPivot(GetBuiltInQuadMesh(), new Vector3(-0.5f, -0.5f, 0));
        meshFilter.mesh = newMesh;
        // 材质手动绑定吧
        MeshRenderer meshRenderer = quadObj.AddComponent<MeshRenderer>();
        // meshRenderer.material = ;

        // 4. 添加Box Collider（可选，适配碰撞检测，大小匹配Quad宽高）
        BoxCollider boxCollider = quadObj.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(1, 1, 0.01f); // 极薄的碰撞体，贴合Quad
        boxCollider.center = new Vector3(0.5f, 0.5f, 0); // 碰撞体中心对齐Quad几何中心

        // 保存到资产目录
        SaveMeshToAsset(newMesh, newMesh.name);

        // 6. 编辑器聚焦到生成的Quad
        Selection.activeGameObject = quadObj;
        EditorGUIUtility.PingObject(quadObj);
    }

    /// <summary>
    /// 生成新Mesh，将轴心点移动到目标位置（基于原Mesh的局部坐标）
    /// </summary>
    /// <param name="originalMesh">原始Mesh</param>
    /// <param name="newPivotLocalPos">新轴心在原Mesh局部空间中的坐标</param>
    /// <returns>轴心点已调整的新Mesh</returns>
    private static Mesh CreateMeshWithNewPivot(Mesh originalMesh, Vector3 newPivotLocalPos) {
        Mesh newMesh = new Mesh();
        newMesh.name = "TileQuad";

        // 1. 计算顶点偏移量：将新轴心点移到模型空间原点
        Vector3 pivotOffset = -newPivotLocalPos;

        // 2. 偏移所有顶点坐标
        Vector3[] vertices = originalMesh.vertices;
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] += pivotOffset;
        }

        // 3. 复制原Mesh的其他数据（三角面、UV、法线等）
        newMesh.vertices = vertices;
        newMesh.triangles = originalMesh.triangles;
        newMesh.uv = originalMesh.uv;
        newMesh.normals = originalMesh.normals;
        newMesh.tangents = originalMesh.tangents;
        newMesh.colors = originalMesh.colors;

        // 4. 重新计算包围盒和绑定信息
        newMesh.RecalculateBounds();
        newMesh.RecalculateTangents();
        newMesh.RecalculateNormals();
        return newMesh;
    }

    /// <summary>
    /// 提取Unity内置的Quad Mesh
    /// </summary>
    private static Mesh GetBuiltInQuadMesh() {
        GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshFilter meshFilter = tempQuad.GetComponent<MeshFilter>();
        Mesh builtInQuadMesh = meshFilter.sharedMesh; // 使用sharedMesh，以避免拷贝
        Object.DestroyImmediate(tempQuad);
        return builtInQuadMesh;
    }

    private static void SaveMeshToAsset(Mesh mesh, string meshName) {
        if (!Directory.Exists("Assets/Resources/Meshes")) {
            Directory.CreateDirectory("Assets/Resources/Meshes");
        }
        string meshPath = $"Assets/Resources/Meshes/{meshName}.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.Refresh();
    }
}
}