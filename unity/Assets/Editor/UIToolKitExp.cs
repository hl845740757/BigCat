using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public class UIToolKitExp : EditorWindow
{
    [MenuItem("Window/UI Toolkit/UIToolKitExp")]
    public static void ShowExample()
    {
        UIToolKitExp wnd = GetWindow<UIToolKitExp>();
        wnd.titleContent = new GUIContent("UIToolKitExp");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/UIToolKitExp.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
    }
}