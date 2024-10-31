using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;

public class MapleOptimizerGUI : EditorWindow
{
    [MenuItem("MapleToolkit/MapleOptimizerGUI")]
    private static void ShowWindow()
    {
        var window = GetWindow<MapleOptimizerGUI>();
        window.titleContent = new GUIContent("MapleOptimizerGUI");
        window.Show();
    }

    public void DrawSplitLine()
    {
        // Color oldColor = GUI.color;
        // GUI.color = Color.white;
        // GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        // GUI.color = oldColor;
        Color lineColor = Color.black; // 这里设置为红色

        // 保存当前颜色
        Color oldColor = GUI.backgroundColor;

        // 设置新的GUI内容颜色
        GUI.backgroundColor = lineColor;
        // 绘制分割线
        // EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        // 恢复原来的GUI内容颜色
        GUI.backgroundColor = oldColor;
    }

    public void RefreshParam()
    {
        availavleParameters = MapleOptimizerCore.SearchAvaliableParameter(avatar);
        quant = new int[availavleParameters.Count];
        selections = new bool[availavleParameters.Count];
        for (int i = 0; i < quant.Length; i++)
            quant[i] = 7;
    }

    public void RefreshOptimized()
    {
        optimizedParameters = MapleOptimizerCore.SearchOptimizedParameter(avatar);
        selections1 = new bool[optimizedParameters.Count];

    }

    VRCAvatarDescriptor lastAvatar;
    VRCAvatarDescriptor avatar;
    List<MapleOptimizerCore.Parameter> availavleParameters;
    List<MapleOptimizerCore.Parameter> optimizedParameters;
    bool[] selections = null;
    bool[] selections1 = null;
    int[] quant = null;
    Vector2 scroll1;
    Vector2 scroll2;
    Vector2 scroll3;
    string kw;
    private void OnGUI()
    {
        int optimizedSize=0;
        minSize = new Vector2(500, 600);
        maxSize = new Vector2(500, 1800);
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        int fontSize = GUI.skin.label.fontSize;
        TextAnchor anchor = GUI.skin.label.alignment;
        TextAnchor anchor1 = GUI.skin.toggle.alignment;
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("参数转换器");

        GUI.skin.label.fontSize = 14;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("       by楓苓MapleRin");
        EditorGUILayout.HelpBox("该插件原理是通过使用bool序列替换掉原有int和float参数来实现操作的,可以降低参数占用", MessageType.Info);
        GUILayout.Space(10);
        GUI.skin.label.fontSize = 16;
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        GUILayout.Label("选择需要进行操作的虚拟形象");
        avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("当前虚拟形象", avatar, typeof(VRCAvatarDescriptor), true);
        if (lastAvatar != avatar)
        {
            if (avatar != null)
            {
                RefreshParam();
                RefreshOptimized();
            }
            lastAvatar = avatar;
        }
        scroll3 = GUILayout.BeginScrollView(scroll3);
        if (avatar != null)
        {
            GUILayout.Space(10);
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("当前可转换的参数：");
            DrawSplitLine();
            if (GUILayout.Button("刷新列表"))
            {
                RefreshParam();
            }
            if (availavleParameters != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(kw != null && !"".Equals(kw) ? "全选当前" : "全选", GUILayout.Width(100)))
                {
                    if (kw != null && !"".Equals(kw))
                    {
                        for (int i = 0; i < selections.Length; i++)
                        {
                            if (!availavleParameters[i].name.ToLower().Contains(kw.ToLower()))
                            {
                                continue;
                            }
                            selections[i] = true;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < selections.Length; i++)
                        {
                            selections[i] = true;
                        }
                    }
                }
                if (GUILayout.Button(kw != null && !"".Equals(kw) ? "取消当前" : "取消全选", GUILayout.Width(100)))
                {
                    if (kw != null && !"".Equals(kw))
                    {
                        for (int i = 0; i < selections.Length; i++)
                        {
                            if (!availavleParameters[i].name.ToLower().Contains(kw.ToLower()))
                            {
                                continue;
                            }
                            selections[i] = false;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < selections.Length; i++)
                        {
                            selections[i] = false;
                        }
                    }
                }
                kw = GUILayout.TextField(kw);
                GUILayout.EndHorizontal();
                scroll1 = GUILayout.BeginScrollView(scroll1, GUILayout.Height(300));
                for (int i = 0; i < availavleParameters.Count; i++)
                {
                    if (kw != null && !"".Equals(kw) && !availavleParameters[i].name.ToLower().Contains(kw.ToLower()))
                    {
                        continue;
                    }
                    GUILayout.BeginHorizontal();
                    // selections[i] = EditorGUILayout.Toggle(selections[i],GUILayout.Width(50));
                    // GUILayout.Space(10);
                    GUI.skin.label.fontSize = 16;
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label("[" + (availavleParameters[i].type == MapleOptimizerCore.ValueType.Float ? "Float" : "Int") + "]参数名:", GUILayout.Width(150));
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(availavleParameters[i].name);
                    selections[i] = GUILayout.Toggle(selections[i], selections[i] ? "[√]" : "[选中]", GUILayout.Width(50));
                    // EditorGUILayout.HelpBox("["+(availavleParameters[i].type==MapleOptimizerCore.ValueType.Float?"Float":"Bool")+"]param:" + availavleParameters[i].name, MessageType.Info);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField("参数所在物体", availavleParameters[i].fromModule, typeof(GameObject), true, GUILayout.Width(350));
                    GUILayout.EndHorizontal();
                    if (selections[i] && availavleParameters[i].type == MapleOptimizerCore.ValueType.Float)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        quant[i] = EditorGUILayout.IntSlider("将被压缩到的参数大小", quant[i], 1, 7);
                        GUILayout.EndHorizontal();

                        EditorGUILayout.HelpBox(quant[i] >= 4 ? "适度调整可以在保证模型重现在远端玩家的质量" : "过度压缩可能会影响远端玩家查看虚拟形象的流畅度", quant[i] >= 4 ? MessageType.None : MessageType.Warning);
                    }
                    DrawSplitLine();
                    // GUILayout.Space(20);

                }
                GUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("暂无可优化的参数", MessageType.Info);
            }
            foreach (bool b in selections)
                if (b)
                {
                    if (GUILayout.Button("开始转换"))
                    {
                        EditorUtility.DisplayDialog("转换器返回信息", MapleOptimizerCore.ParameterConvertor(avatar, availavleParameters, selections, quant), "确认");
                        RefreshParam();
                        RefreshOptimized();
                    }
                    break;
                }

            GUILayout.Space(10);
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("已转换的参数：");
            DrawSplitLine();
            if (GUILayout.Button("刷新列表"))
            {
                RefreshOptimized();
            }
            if (optimizedParameters != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("全选", GUILayout.Width(100)))
                {
                    for (int i = 0; i < selections1.Length; i++)
                    {
                        selections1[i] = true;
                    }
                }
                if (GUILayout.Button("取消全选", GUILayout.Width(100)))
                {
                    for (int i = 0; i < selections1.Length; i++)
                    {
                        selections1[i] = false;
                    }
                }
                GUILayout.EndHorizontal();
                scroll2 = GUILayout.BeginScrollView(scroll2, GUILayout.Height(300));
                for (int i = 0; i < optimizedParameters.Count; i++)
                {

                    GUILayout.BeginHorizontal();
                    // selections[i] = EditorGUILayout.Toggle(selections[i],GUILayout.Width(50));
                    // GUILayout.Space(10);
                    GUI.skin.label.fontSize = 16;
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label("参数名:", GUILayout.Width(150));
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(optimizedParameters[i].name);
                    selections1[i] = GUILayout.Toggle(selections1[i], selections1[i] ? "[√]" : "[选中]", GUILayout.Width(50));
                    // EditorGUILayout.HelpBox("["+(availavleParameters[i].type==MapleOptimizerCore.ValueType.Float?"Float":"Bool")+"]param:" + availavleParameters[i].name, MessageType.Info);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    // selections[i] = EditorGUILayout.Toggle(selections[i],GUILayout.Width(50));
                    // GUILayout.Space(10);
                    GUI.skin.label.fontSize = 14;
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label("优化后占用:("+optimizedParameters[i].optimizedSize+"/8)", GUILayout.Width(150));
                    optimizedSize+=8-optimizedParameters[i].optimizedSize;
                    GUILayout.EndHorizontal();
                    DrawSplitLine();
                }
                GUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("该虚拟形象没有已转换参数", MessageType.Info);
            }
            foreach (bool b in selections1)
                if (b)
                {
                    if (GUILayout.Button("恢复参数"))
                    {
                        for(int i=0;i<optimizedParameters.Count;i++){
                            if(selections1[i]){
                                MapleOptimizerCore.RemoveParameterConvertor(avatar,optimizedParameters[i].name);
                            }
                        }
                        EditorUtility.DisplayDialog("转换器移除完成", "已移除选中的参数转换器", "确认");
                        RefreshParam();
                        RefreshOptimized();
                    }
                    break;
                }
        }
        GUILayout.Space(50);
        GUILayout.EndScrollView ();
        if(optimizedParameters!=null)
            EditorGUILayout.HelpBox("参数优化器已节省"+optimizedSize+"bits 参数", MessageType.Info);
        GUILayout.EndVertical();
        
        GUI.skin.toggle.alignment = TextAnchor.MiddleCenter;
        GUI.skin.label.fontSize = fontSize;
        GUI.skin.label.alignment = anchor;
        GUI.skin.toggle.alignment = anchor1;
    }
}

