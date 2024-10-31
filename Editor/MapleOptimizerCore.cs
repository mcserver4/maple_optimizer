using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

public class MapleOptimizerCore : MonoBehaviour
{
    public enum ValueType
    {
        NotSync = 0,
        Bool = 1,
        Float = 2,
        Int = 3
    };

    public struct Parameter
    {
        public GameObject fromModule;
        public string name;
        public ValueType type;
        public int optimizedSize;
        public bool saved;
    };

    static string rootPath = "Assets/MapleParamOptimizer";

    public static List<Parameter> SearchAvaliableParameter(VRCAvatarDescriptor avatar)
    {
        if (avatar == null)
            return null;
        List<Parameter> parameters = new List<Parameter>();
        EditorUtility.DisplayProgressBar("搜索可转换参数中", "", 0);
        if (avatar.expressionParameters == null)
        {
            return parameters;
        }
        VRCExpressionParameters.Parameter[] avatarIntegratedParameter = avatar.expressionParameters.parameters;
        foreach (VRCExpressionParameters.Parameter p in avatarIntegratedParameter)
        {

            if (p.networkSynced && (p.valueType == VRCExpressionParameters.ValueType.Int || p.valueType == VRCExpressionParameters.ValueType.Float))
            {
                EditorUtility.DisplayProgressBar("搜索可转换参数中", "搜索参数:" + p.name, 0.5f);
                // Debug.Log(p.name);
                parameters.Add(new Parameter
                {
                    fromModule = avatar.gameObject,
                    name = p.name,
                    type = p.valueType == VRCExpressionParameters.ValueType.Int ? ValueType.Int : ValueType.Float,
                    saved = p.saved
                });
            }
        }

        SearchMAParameter(avatar.transform, parameters);
        EditorUtility.ClearProgressBar();
        return parameters;
    }

    public static void SearchMAParameter(Transform parentTransform, List<Parameter> list)
    {
        if (parentTransform.TryGetComponent<ModularAvatarParameters>(out ModularAvatarParameters parametersComp) && parametersComp.parameters != null)
        {
            foreach (ParameterConfig conf in parametersComp.parameters)
            {
                if (!conf.localOnly && conf.syncType != ParameterSyncType.NotSynced && conf.syncType != ParameterSyncType.Bool)
                {
                    // Debug.Log(parentTransform.name+"_>"+conf.nameOrPrefix);
                    EditorUtility.DisplayProgressBar("搜索可转换参数中", "搜索MA部件:" + conf.nameOrPrefix, 0.75f);
                    if (!list.Any(p => p.name.Equals(conf.nameOrPrefix)))
                        list.Add(new Parameter
                        {
                            fromModule = parentTransform.gameObject,
                            name = conf.nameOrPrefix,
                            type = conf.syncType == ParameterSyncType.Int ? ValueType.Int : ValueType.Float,
                            saved = conf.saved
                        });
                }
            }
        }
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform c = parentTransform.GetChild(i);
            SearchMAParameter(c, list);
        }
    }


    //int searchStart

    public static void SearchMAComponent(Transform parentTransform, string paramName, HashSet<int> list)
    {
        if (parentTransform.name.Equals("ParamOptimizer")) return;
        if (parentTransform.TryGetComponent<ModularAvatarMenuInstaller>(out ModularAvatarMenuInstaller installer))
        {
            if (installer.menuToAppend != null)
            {
                CheckMenuAppRevInt(installer.menuToAppend, paramName, list);
            }
        }
        if (parentTransform.TryGetComponent<ModularAvatarMergeAnimator>(out ModularAvatarMergeAnimator mergeAnimator))
        {
            ModularAvatarMergeAnimator[] mArray = parentTransform.GetComponents<ModularAvatarMergeAnimator>();
            foreach (ModularAvatarMergeAnimator m in mArray)
                if (m.animator != null)
                {
                    // Debug.Log("搜索:"+m.animator.name);
                    SearchAnimator((AnimatorController)m.animator, paramName, list);
                }
        }
        if (parentTransform.TryGetComponent<ModularAvatarMenuItem>(out ModularAvatarMenuItem item))
        {
            if (item.Control.parameter != null && item.Control.parameter.name.Equals(paramName) && (item.Control.type == VRCExpressionsMenu.Control.ControlType.Button || item.Control.type == VRCExpressionsMenu.Control.ControlType.Toggle))
            {
                list.Add(0);
                list.Add((int)item.Control.value);
            }
        }


        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform c = parentTransform.GetChild(i);
            SearchMAComponent(c, paramName, list);
        }
    }

    public static void SearchAnimator(AnimatorController controller, string paramName, HashSet<int> list)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        foreach (AnimatorControllerLayer layer in layers)
        {
            SearchStateMachine(layer.stateMachine, paramName, list);
        }
    }

    public static void CheckBehaviour(StateMachineBehaviour[] behaviours, string paramName, HashSet<int> list)
    {

        foreach (StateMachineBehaviour behaviour in behaviours)
        {

            if (behaviour.GetType().Equals(typeof(VRCAvatarParameterDriver)))
            {

                VRCAvatarParameterDriver driver = (VRCAvatarParameterDriver)behaviour;
                // Debug.Log(behaviour+" "+driver.isEnabled);
                if (driver.parameters != null)
                {
                    foreach (VRC.SDKBase.VRC_AvatarParameterDriver.Parameter p in driver.parameters)
                    {
                        // Debug.Log("--" + p.name);
                        if (paramName.Equals(p.name) || paramName.Equals(p.source))
                        {
                            switch (p.type)
                            {
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set:
                                    {
                                        list.Add((int)p.value);
                                    }
                                    break;
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Add:
                                    {
                                        throw new Exception("不可转换的参数:暂不支持带自增(减)功能的参数");
                                    }
                                // break;
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Copy:
                                    {
                                        if (paramName.Equals(p.name))
                                            throw new Exception("不可转换的参数:暂不支持被其它参数映射的参数");
                                        break;
                                    }
                                // break;
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random:
                                    {
                                        for (int i = (int)p.valueMin; i < (int)p.valueMax; i++)
                                        {
                                            list.Add(i);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void CheckMenuApperanceInt(VRCAvatarDescriptor avatar, string paramName, HashSet<int> list)
    {
        if (avatar == null)
        {
            return;
        }
        VRCExpressionsMenu root = avatar.expressionsMenu;
        CheckMenuAppRevInt(root, paramName, list);

    }

    public static void CheckMenuAppRevInt(VRCExpressionsMenu par, string paramName, HashSet<int> list)
    {
        List<VRCExpressionsMenu.Control> controls = par.controls;
        if (controls != null)
        {
            foreach (VRCExpressionsMenu.Control c in controls)
            {
                if (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu != null)
                {
                    CheckMenuAppRevInt(c.subMenu, paramName, list);
                }
                if (c.parameter != null && c.parameter.name.Equals(paramName) && (c.type == VRCExpressionsMenu.Control.ControlType.Toggle || c.type == VRCExpressionsMenu.Control.ControlType.Button))
                {
                    list.Add(0);
                    list.Add((int)c.value);
                }
            }
        }
    }

    public static void SearchStateMachine(AnimatorStateMachine parentStateMachine, string paramName, HashSet<int> list)
    {
        StateMachineBehaviour[] behaviours = parentStateMachine.behaviours;
        ChildAnimatorState[] states = parentStateMachine.states;
        ChildAnimatorStateMachine[] pstateMachine = parentStateMachine.stateMachines;
        if (behaviours != null)
        {
            CheckBehaviour(behaviours, paramName, list);
        }
        if (states != null)
        {
            foreach (ChildAnimatorState s in states)
            {
                AnimatorState state = s.state;
                StateMachineBehaviour[] behaviours1 = state.behaviours;
                CheckBehaviour(behaviours1, paramName, list);
            }
        }
        if (pstateMachine != null)
        {
            foreach (ChildAnimatorStateMachine s in pstateMachine)
            {
                SearchStateMachine(s.stateMachine, paramName, list);
            }
        }
    }

    public static List<int> SearchIntValuesApp(VRCAvatarDescriptor avatar, string paramName)
    {
        if (avatar == null)
            return null;
        HashSet<int> values = new HashSet<int>();
        foreach (var layer in avatar.baseAnimationLayers)
        {
            if (layer.animatorController != null)
            {
                SearchAnimator((AnimatorController)layer.animatorController, paramName, values);
            }
        }

        CheckMenuApperanceInt(avatar, paramName, values);
        SearchMAComponent(avatar.transform, paramName, values);
        return values.ToList();
    }
    //int searchEnd






    //float searchStart
    static bool globalFlag;//is signed float when true

    public static void CheckBehaviourFloat(StateMachineBehaviour[] behaviours, string paramName)
    {

        foreach (StateMachineBehaviour behaviour in behaviours)
        {

            if (behaviour.GetType().Equals(typeof(VRCAvatarParameterDriver)))
            {

                VRCAvatarParameterDriver driver = (VRCAvatarParameterDriver)behaviour;
                // Debug.Log(behaviour+" "+driver.isEnabled);
                if (driver.parameters != null)
                {
                    foreach (VRC.SDKBase.VRC_AvatarParameterDriver.Parameter p in driver.parameters)
                    {

                        if (paramName.Equals(p.name) || paramName.Equals(p.source))
                        {
                            // Debug.Log("--n" + p.name+" dstp"+p.destParam+" src"+p.source+" srcp"+p.sourceParam+" ");
                            switch (p.type)
                            {
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set:
                                    {
                                        if (p.value < 0)
                                        {
                                            globalFlag = true;
                                            return;
                                        }
                                    }
                                    break;
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Add:
                                    {
                                        throw new Exception("不可转换的参数:暂不支持带自增(减)功能的参数");
                                    }
                                // break;
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Copy:
                                    {
                                        if (paramName.Equals(p.source))
                                        {
                                            if (p.sourceMin < 0 || p.sourceMax < 0)
                                            {
                                                globalFlag = true;
                                                return;
                                            }
                                        }
                                        else if (paramName.Equals(p.name))
                                        {
                                            if (p.destMin < 0 || p.destMax < 0)
                                            {
                                                globalFlag = true;
                                                return;
                                            }
                                        }
                                        break;
                                    }
                                // break;
                                case VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random:
                                    {
                                        if (p.valueMin < 0 || p.valueMax < 0)
                                        {
                                            globalFlag = true;
                                            return;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void SearchAnimatorFloat(AnimatorController controller, string paramName)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        foreach (AnimatorControllerLayer layer in layers)
        {
            SearchStateMachineFloat(layer.stateMachine, paramName);
        }
    }

    public static void SearchStateMachineFloat(AnimatorStateMachine parentStateMachine, string paramName)
    {
        if (globalFlag) return;
        StateMachineBehaviour[] behaviours = parentStateMachine.behaviours;
        ChildAnimatorState[] states = parentStateMachine.states;
        ChildAnimatorStateMachine[] pstateMachine = parentStateMachine.stateMachines;
        if (behaviours != null)
        {
            CheckBehaviourFloat(behaviours, paramName);
            if (globalFlag) return;
        }
        if (states != null)
        {
            foreach (ChildAnimatorState s in states)
            {
                AnimatorState state = s.state;
                StateMachineBehaviour[] behaviours1 = state.behaviours;
                CheckBehaviourFloat(behaviours1, paramName);
                if (globalFlag) return;
            }
        }
        if (pstateMachine != null)
        {
            foreach (ChildAnimatorStateMachine s in pstateMachine)
            {

                SearchStateMachineFloat(s.stateMachine, paramName);
                if (globalFlag) return;
            }
        }
    }


    public static void CheckMenuApperanceFloat(VRCAvatarDescriptor avatar, string paramName)
    {
        if (avatar == null)
        {
            return;
        }
        VRCExpressionsMenu root = avatar.expressionsMenu;
        CheckMenuAppRevFloat(root, paramName);
    }

    public static void CheckMenuAppRevFloat(VRCExpressionsMenu par, string paramName)
    {
        if (globalFlag)
        {
            return;
        }
        List<VRCExpressionsMenu.Control> controls = par.controls;
        if (controls != null)
        {
            foreach (VRCExpressionsMenu.Control c in controls)
            {
                if (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu != null)
                {
                    CheckMenuAppRevFloat(c.subMenu, paramName);
                }
                if (c.type == VRCExpressionsMenu.Control.ControlType.FourAxisPuppet)
                {
                    if (c.subParameters != null)
                    {
                        foreach (VRCExpressionsMenu.Control.Parameter p in c.subParameters)
                        {
                            if (paramName.Equals(p.name))
                            {
                                globalFlag = true;
                                if (globalFlag)
                                {
                                    return;
                                }
                                break;
                            }
                        }
                    }
                }

            }
        }
    }

    public static void SearchMAComponentFloat(Transform parentTransform, string paramName)
    {
        if (globalFlag || parentTransform.name.Equals("ParamOptimizer")) return;
        if (parentTransform.TryGetComponent<ModularAvatarMenuInstaller>(out ModularAvatarMenuInstaller installer))
        {
            if (installer.menuToAppend != null)
            {
                CheckMenuAppRevFloat(installer.menuToAppend, paramName);
            }
        }
        if (parentTransform.TryGetComponent<ModularAvatarMergeAnimator>(out ModularAvatarMergeAnimator mergeAnimator))
        {
            if (mergeAnimator.animator != null)
            {
                SearchAnimatorFloat((AnimatorController)mergeAnimator.animator, paramName);
            }
        }
        if (parentTransform.TryGetComponent<ModularAvatarMenuItem>(out ModularAvatarMenuItem item))
        {
            if (item.Control.type == VRCExpressionsMenu.Control.ControlType.FourAxisPuppet)
            {
                foreach (var p in item.Control.subParameters)
                {
                    if (paramName.Equals(p.name))
                    {
                        globalFlag = true;
                        return;
                    }
                }
            }
        }


        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform c = parentTransform.GetChild(i);
            SearchMAComponentFloat(c, paramName);
        }
    }

    public static bool UsingSignedFloat(VRCAvatarDescriptor avatar, string paramName)
    {
        globalFlag = false;
        foreach (var layer in avatar.baseAnimationLayers)
        {
            if (layer.animatorController != null)
            {
                SearchAnimatorFloat((AnimatorController)layer.animatorController, paramName);
            }
        }
        CheckMenuApperanceFloat(avatar, paramName);
        SearchMAComponentFloat(avatar.transform, paramName);
        return globalFlag;
    }

    //float searchEnd

    public static bool SetOriginalToLocal(VRCAvatarDescriptor avatar, Parameter parameter)
    {
        if (parameter.fromModule != avatar.gameObject)
        {
            if (parameter.fromModule.TryGetComponent<ModularAvatarParameters>(out var parameters))
            {
                for (int i = 0; i < parameters.parameters.Count; i++)
                {
                    if (parameters.parameters[i].nameOrPrefix.Equals(parameter.name))
                    {
                        EditorUtility.SetDirty(parameters);
                        ParameterConfig conf = parameters.parameters[i];
                        conf.localOnly = true;
                        parameters.parameters[i] = conf;
                        AssetDatabase.SaveAssetIfDirty(parameters);
                        return true;
                    }
                }
            }
        }
        else
        {
            if (avatar.expressionParameters != null)
            {
                for (int i = 0; i < avatar.expressionParameters.parameters.Length; i++)
                {
                    if (avatar.expressionParameters.parameters[i].name.Equals(parameter.name))
                    {
                        EditorUtility.SetDirty(avatar.expressionParameters);
                        avatar.expressionParameters.parameters[i].networkSynced = false;
                        AssetDatabase.SaveAssetIfDirty(avatar.expressionParameters);
                        return true;
                    }
                }
            }
        }
        return false;
    }


    public static void SearchResetParameter(VRCAvatarDescriptor avatar, string paramName)
    {
        if (avatar == null)
            return;
        List<Parameter> parameters = new List<Parameter>();
        if (avatar.expressionParameters == null)
        {
            return;
        }
        VRCExpressionParameters.Parameter[] avatarIntegratedParameter = avatar.expressionParameters.parameters;
        for (int i = 0; i < avatarIntegratedParameter.Length; i++)
        {
            VRCExpressionParameters.Parameter p = avatarIntegratedParameter[i];
            if (!p.networkSynced && p.name.Equals(paramName) && (p.valueType == VRCExpressionParameters.ValueType.Int || p.valueType == VRCExpressionParameters.ValueType.Float))
            {
                EditorUtility.SetDirty(avatar.expressionParameters);
                avatar.expressionParameters.parameters[i].networkSynced = true;
                AssetDatabase.SaveAssetIfDirty(avatar.expressionParameters);
                return;
            }
        }
        bool dummy = false;
        SearchResetMAParameter(avatar.transform, paramName, ref dummy);
    }

    public static void SearchResetMAParameter(Transform parentTransform, string paramName, ref bool rstFlag)
    {
        if (rstFlag) return;
        if (parentTransform.TryGetComponent<ModularAvatarParameters>(out ModularAvatarParameters parametersComp) && parametersComp.parameters != null)
        {
            for (int i = 0; i < parametersComp.parameters.Count; i++)
            {
                ParameterConfig conf = parametersComp.parameters[i];
                if (conf.localOnly && conf.nameOrPrefix.Equals(paramName) && conf.syncType != ParameterSyncType.NotSynced && conf.syncType != ParameterSyncType.Bool)
                {
                    // Debug.Log(parentTransform.name+"_>"+conf.nameOrPrefix);
                    EditorUtility.SetDirty(parametersComp);
                    conf.localOnly = false;
                    parametersComp.parameters[i] = conf;
                    AssetDatabase.SaveAssetIfDirty(parametersComp);
                    return;
                }
            }
        }
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform c = parentTransform.GetChild(i);
            SearchResetMAParameter(c, paramName, ref rstFlag);
            if (rstFlag) return;
        }
    }

    public static void RemoveParameterConvertor(VRCAvatarDescriptor avatar, string paramName)
    {
        ControllerValidation(avatar, out var avatarParameters, out var animMerge);
        AnimatorController animatorController = (AnimatorController)animMerge.animator;
        avatarParameters.parameters.RemoveAll(p => p.nameOrPrefix.StartsWith(paramName + "_syncBool"));
        List<int> remID = new List<int>();
        AnimatorControllerParameter[] p = animatorController.parameters;

        var pl = p.ToList();
        pl.RemoveAll(p => p.name.StartsWith(paramName));
        animatorController.parameters = pl.ToArray();

        // AnimationClip empty = AssetDatabase.LoadAssetAtPath<AnimationClip>(rootPath+"/Empty.anim");
        // AnimationClip empty = Resources.Load<AnimationClip>("empty");

        AnimatorControllerLayer[] ls = animatorController.layers;
        // ArrayUtility.Remove(ref ls,)
        var l = ls.ToList();
        l.RemoveAll(p => ("EncodeLayer_" + paramName).Equals(p.name) || ("DecodeLayer_" + paramName).Equals(p.name));
        animatorController.layers = l.ToArray();

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssetIfDirty(animatorController);

        SearchResetParameter(avatar, paramName);
    }

    public static List<Parameter> SearchOptimizedParameter(VRCAvatarDescriptor avatar)
    {
        if (avatar == null) return null;
        List<Parameter> parameters = new List<Parameter>();
        ControllerValidation(avatar, out var avatarParameters, out var animMerge);
        string[] names = (animMerge.animator as AnimatorController).parameters.Select(p => p.name).ToArray();
        var namesList = names.ToList();
        namesList.RemoveAll(p => !p.Contains("_syncBool"));
        names = namesList.ToArray();

        Dictionary<string, int> dict = new Dictionary<string, int>();
        foreach (var name in names)
        {
            int stInd = name.IndexOf("_syncBool");
            string paramName = name.Substring(0, stInd);
            if (dict.ContainsKey(paramName))
            {
                dict[paramName]++;
            }
            else
            {
                dict.Add(paramName, 1);
            }
        }

        return dict.Select(p => new Parameter
        {
            name = p.Key,
            optimizedSize = p.Value
        }).ToList();
    }


    public static string ParameterConvertor(VRCAvatarDescriptor avatar, List<Parameter> parameters, bool[] selection, int[] quants)
    {
        if (avatar == null || parameters == null || selection == null || quants == null || parameters.Count != selection.Length || parameters.Count != quants.Length)
        {
            return "传入参数无效";
        }
        // Dictionary<string, List<int>> intDict = new Dictionary<string, List<int>>();
        ControllerValidation(avatar, out var para, out var animator);

        int count=0;
        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i])
            {
                
                bool flag=true;
                switch (parameters[i].type)
                {
                    case ValueType.Int:
                        {
                            try
                            {
                                List<int> appInt = SearchIntValuesApp(avatar, parameters[i].name);
                                if(appInt.Count==0)
                                    throw new Exception("未找到有效参数");
                                List<float> appFloat = new List<float>();
                                foreach (int appIntValue in appInt)
                                {
                                    appFloat.Add(appIntValue);
                                    // Debug.Log(parameters[i].name + "出现的数字:" + appIntValue);
                                }
                                //generate encode decode layer
                                SetupEncoder((AnimatorController)animator.animator, para, appFloat, parameters[i].name, false, 0);
                                count++;
                            }
                            catch (Exception e)
                            {
                                Debug.LogError("参数:" + parameters[i].name + " 转换终止。终止原因:" + e.Message);
                                Debug.LogError("栈追踪信息:"+e.StackTrace);
                                flag=false;
                            }
                            break;
                        }
                    case ValueType.Float:
                        {
                            try
                            {
                                bool usingSigned = UsingSignedFloat(avatar, parameters[i].name);
                                int maxSplit = (int)Mathf.Pow(2, quants[i] - (usingSigned ? 1 : 0));
                                float step = 1f / maxSplit;
                                // Debug.Log("参数" + parameters[i].name + " " + usingSigned + " step:" + 1f / maxSplit + " splitNum" + maxSplit);
                                List<float> appFloat = new List<float>();
                                for (int j = 0; j < maxSplit; j++)
                                {
                                    appFloat.Add(Mathf.Lerp(usingSigned ? -1 : 0, 1, j / (float)(maxSplit - 1)));
                                    // if (usingSigned)
                                    //     Debug.Log("sp:" + Mathf.Lerp(-1, 1, j / (float)(maxSplit - 1)));
                                    // else
                                    //     Debug.Log("p:" + Mathf.Lerp(0, 1, j / (float)(maxSplit - 1)));
                                }
                                //generate encode decode layer
                                count++;
                                SetupEncoder((AnimatorController)animator.animator, para, appFloat, parameters[i].name, true, step);

                            }
                            catch (Exception e)
                            {
                                Debug.LogError("参数:" + parameters[i].name + " 转换终止。终止原因:" + e.Message);
                                Debug.LogError("栈追踪信息:"+e.StackTrace);
                                flag=false;
                            }
                            break;
                        }
                }
                if(flag)
                    SetOriginalToLocal(avatar, parameters[i]);
            }
        }
        return "转换了"+count+"个参数";
    }

    public static bool[] ConvertIntToByte(int input, int maxByte)
    {
        bool[] bytes = new bool[maxByte];
        for (int i = bytes.Length - 1; i >= 0; i--)
        {
            bytes[i] = input % 2 == 1;
            input >>= 1;
        }
        return bytes;
    }
    
    public static int GetMaxByte(int maxInt){
        //1,2,3,4,5,6,7,8
        // int[] ints = {2,4,8,16,32,64,128,256};
        int i=1;
        for(;maxInt>(int)Mathf.Pow(2,i);i++);
        return i;
    }
    public static int GetMaxByteNone(int maxInt)
    {
        // Debug.Log("max:"+maxInt);
        int byteCount = 0;
        while (maxInt > 0 && byteCount <= 8)
        {
            maxInt >>= 1;
            byteCount++;
            // Debug.Log("max:"+maxInt+" bc:"+byteCount);
        }
        return byteCount;
    }

    public static void ControllerValidation(VRCAvatarDescriptor avatar, out ModularAvatarParameters pa, out ModularAvatarMergeAnimator an)
    {
        //obj name:ParamOptimizer
        Transform t = avatar.transform.Find("ParamOptimizer");
        if (t == null)
        {
            t = new GameObject("ParamOptimizer").transform;
            t.parent = avatar.transform;
            t.localPosition = Vector3.zero;
        }
        if (!t.gameObject.TryGetComponent<ModularAvatarParameters>(out var p))
        {
            p = t.gameObject.AddComponent<ModularAvatarParameters>();
        }
        if (!t.gameObject.TryGetComponent<ModularAvatarMergeAnimator>(out var c))
        {
            c = t.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            c.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        }
        CreateFolder("Assets/OptimizedParamAnimator");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/OptimizedParamAnimator/Controller_" + avatar.name.GetHashCode() + ".controller");
        if (controller == null)
        {
            AnimatorController.CreateAnimatorControllerAtPath("Assets/OptimizedParamAnimator/Controller_" + avatar.name.GetHashCode() + ".controller");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/OptimizedParamAnimator/Controller_" + avatar.name.GetHashCode() + ".controller");
        }
        AnimatorControllerParameter[] pl = controller.parameters;
        var p1 = pl.ToList();
        if (p1.Find(pa => pa.name.Equals("IsLocal")) == null)
        {
            controller.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);
        }
        c.animator = controller;
        pa = p;
        an = c;
    }

    public static void SetupEncoder(AnimatorController animatorController, ModularAvatarParameters avatarParameters, List<float> addedMenusID, string paramName, bool isFloat, float minStep)
    {
        if (animatorController == null || addedMenusID == null || addedMenusID.Count <= 0 || avatarParameters == null)
        {
            return;
        }
        
        avatarParameters.parameters.RemoveAll(p => p.nameOrPrefix.StartsWith(paramName + "_syncBool"));
        // List<int> remID = new List<int>();
        AnimatorControllerParameter[] p = animatorController.parameters;

        var pl = p.ToList();
        pl.RemoveAll(p => p.name.StartsWith(paramName));
        animatorController.parameters = pl.ToArray();
        // for(int i=0;i<p.Length;i++){
        //     if(p[i].name.StartsWith(paramName)){
        //         remID.Add(i);
        //     }
        // }
        // foreach(int i in remID)
        //     animatorController.RemoveParameter(i);



        // AnimationClip empty = AssetDatabase.LoadAssetAtPath<AnimationClip>(rootPath+"/Empty.anim");
        AnimationClip empty = Resources.Load<AnimationClip>("Empty");

        AnimatorControllerLayer[] ls = animatorController.layers;
        // ArrayUtility.Remove(ref ls,)
        var l = ls.ToList();
        l.RemoveAll(p => ("EncodeLayer_" + paramName).Equals(p.name) || ("DecodeLayer_" + paramName).Equals(p.name));
        animatorController.layers = l.ToArray();

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssetIfDirty(animatorController);
        // int L1=-1,L2=-1;
        // for (int i=0;i<ls.Length;i++)
        // {
        //     if (("EncodeLayer_"+paramName).Equals(ls[i].name))
        //     {
        //         L1=i;
        //     }
        //     if (("DecodeLayer_"+paramName).Equals(ls[i].name))
        //     {
        //         L2=i;
        //     }
        // }
        // if(L1!=-1)
        //     animatorController.RemoveLayer(L1);
        // if(L2!=-1)
        //     animatorController.RemoveLayer(L2);

        int maxByte = GetMaxByte(addedMenusID.Count);
        Debug.Log("count:"+addedMenusID.Count+" using bits count:"+maxByte);
        for (int i = 0; i < maxByte; i++)
        {
            avatarParameters.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = paramName + "_syncBool" + i,
                syncType = ParameterSyncType.Bool,
                localOnly = false,
                // saved = saved
            });
            animatorController.AddParameter(paramName + "_syncBool" + i, AnimatorControllerParameterType.Bool);
        }
        animatorController.AddLayer("EncodeLayer_" + paramName);
        animatorController.AddLayer("DecodeLayer_" + paramName);
        AnimatorControllerLayer[] layers = animatorController.layers;
        AnimatorControllerLayer encodeLayer = null;
        AnimatorControllerLayer decodeLayer = null;
        foreach (AnimatorControllerLayer layer in layers)
        {
            if (("EncodeLayer_" + paramName).Equals(layer.name))
            {
                encodeLayer = layer;
            }
            if (("DecodeLayer_" + paramName).Equals(layer.name))
            {
                decodeLayer = layer;
            }
        }

        animatorController.AddParameter(paramName, !isFloat ? AnimatorControllerParameterType.Int : AnimatorControllerParameterType.Float);
        decodeLayer.defaultWeight = encodeLayer.defaultWeight = 1.0f;
        animatorController.layers = layers;
        AnimatorStateMachine encodeS = encodeLayer.stateMachine;
        AnimatorStateMachine decodeS = decodeLayer.stateMachine;

        AnimatorState noneS = encodeS.AddState("noneState");
        noneS.speed = 10;
        encodeS.defaultState = noneS;
        AnimatorStateTransition t = noneS.AddExitTransition();
        t.exitTime = 0;
        t.hasExitTime = true;
        t.duration = 0;

        AnimatorState noneS1 = decodeS.AddState("noneState");
        noneS1.speed = 10;
        decodeS.defaultState = noneS1;
        AnimatorStateTransition t1 = noneS1.AddExitTransition();
        t1.exitTime = 0;
        t1.hasExitTime = true;
        t1.duration = 0;


        for (int i = 0; i < addedMenusID.Count; i++)
        {
            bool[] ary = ConvertIntToByte(i, maxByte);
            AnimatorState state = encodeS.AddState("[" + i + "]Num_" + addedMenusID[i].ToString().Replace(".", "_"));
            state.writeDefaultValues = true;
            state.motion = empty;
            state.speed = 100;
            VRCAvatarParameterDriver encDriver = (VRCAvatarParameterDriver)state.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
            encDriver.localOnly = true;
            encDriver.parameters ??= new List<VRC_AvatarParameterDriver.Parameter>();
            AnimatorStateTransition encTransition = encodeS.AddAnyStateTransition(state);
            encTransition.hasExitTime = false;
            encTransition.duration = 0;
            if (isFloat)
            {

                if (i < addedMenusID.Count - 1)
                {
                    encTransition.AddCondition(AnimatorConditionMode.Greater, addedMenusID[i] - minStep * 0.5f, paramName);
                    encTransition.AddCondition(AnimatorConditionMode.Less, addedMenusID[i + 1] - minStep * 0.5f, paramName);
                }
                else
                {
                    encTransition.AddCondition(AnimatorConditionMode.Greater, addedMenusID[i] - minStep * 0.5f, paramName);
                    // encTransition.AddCondition(AnimatorConditionMode.Less, addedMenusID[i+1], paramName);
                }
            }
            else
            {
                encTransition.AddCondition(AnimatorConditionMode.Equals, addedMenusID[i], paramName);
            }

            encTransition.AddCondition(AnimatorConditionMode.If, 1, "IsLocal");

            AnimatorState dState = decodeS.AddState("[" + i + "]Num_" + addedMenusID[i].ToString().Replace(".", "_"));
            dState.writeDefaultValues = true;
            dState.motion = empty;
            dState.speed = 100;
            VRCAvatarParameterDriver decDriver = (VRCAvatarParameterDriver)dState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));

            decDriver.localOnly = false;
            decDriver.parameters ??= new List<VRC_AvatarParameterDriver.Parameter>();
            AnimatorStateTransition decTransition = decodeS.AddAnyStateTransition(dState);
            decTransition.hasExitTime = false;
            decTransition.duration = 0;
            decTransition.AddCondition(AnimatorConditionMode.IfNot, 1, "IsLocal");
            for (int j = 0; j < ary.Length; j++)
            {
                encDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = paramName + "_syncBool" + j,
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    value = ary[j] ? 1 : 0
                });
                decTransition.AddCondition(ary[j] ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 1, paramName + "_syncBool" + j);
            }
            decDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = paramName,
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                value = addedMenusID[i]
            });
            EditorUtility.SetDirty(encDriver);
            EditorUtility.SetDirty(decDriver);
            AssetDatabase.SaveAssets();
        }
        EditorUtility.SetDirty(encodeS);
        EditorUtility.SetDirty(decodeS);
        EditorUtility.SetDirty(animatorController);
        EditorUtility.SetDirty(avatarParameters);
        animatorController.layers = layers;
        AssetDatabase.SaveAssets();
    }

    static string AssociatePath(string[] folderArray, int indLimiter)
    {
        if (folderArray == null || folderArray.Length == 0)
            return "";
        string result = "";
        int a = indLimiter < folderArray.Length ? indLimiter : folderArray.Length - 1;
        // Debug.Log("ind"+a);
        for (int i = 0; i <= a; i++)
        {
            result += folderArray[i] + "/";
        }
        return result;
    }

    /// <summary>
    /// 根据路径创建文件夹，如果路径中有不存在文件夹则创建，已存在文件夹则无视
    /// 必须以Assets文件夹开头
    /// </summary>
    /// <param name="path">要创建的文件夹路径</param>
    public static void CreateFolder(string path)
    {
        string[] store = path.Split("/");
        if (store[0].Equals("Assets"))
        {
            for (int i = 1; i < store.Length; i++)
            {
                if (store[i].Equals(""))
                {
                    continue;
                }
                string assoPath = AssociatePath(store, i);
                if (!AssetDatabase.IsValidFolder(assoPath))
                {
                    Debug.Log("create:" + assoPath);
                    AssetDatabase.CreateFolder(AssociatePath(store, i - 1).TrimEnd('/'), store[i]);
                    // Debug.Log(AssetDatabase.GUIDToAssetPath(guid));
                    AssetDatabase.SaveAssets();

                }
            }
        }
        AssetDatabase.Refresh();
    }
}
