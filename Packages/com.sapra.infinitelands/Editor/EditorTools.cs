using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace sapra.InfiniteLands.Editor
{
    public static class EditorTools
    {
        public static bool DebugMode;
        [MenuItem("Window/Infinite Lands/Debug Mode", false, 10000)]
        public static void ToggleFeature()
        {
            DebugMode = !DebugMode;
        }

        [MenuItem("Window/Infinite Lands/Debug Mode", true)]
        public static bool ToggleFeature_Validate()
        {
            Menu.SetChecked("Window/Infinite Lands/Debug Mode", DebugMode);
            return true;
        }

        public const string PortStyles = "Packages/com.sapra.infinitelands/Editor/UIBuilder/Port.uss";

        public static string GetNodeMenuType(CustomNodeAttribute attribute, Type type)
        {
            if (!string.IsNullOrEmpty(attribute.customType))
                return attribute.customType;

            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:script");
            if (guids.Length > 0)
            {
                string targetGuid = guids.FirstOrDefault(a => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(a)) == type.Name);
                string assetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
                string folderPath = Path.GetDirectoryName(assetPath);
                string nodesMarker = "Nodes" + Path.DirectorySeparatorChar;
                int nodesIndex = folderPath.IndexOf(nodesMarker, StringComparison.OrdinalIgnoreCase);

                if (nodesIndex >= 0)
                {
                    return folderPath.Substring(nodesIndex + nodesMarker.Length)
                        .Replace(Path.DirectorySeparatorChar, '/');
                }
                return folderPath.Replace(Path.DirectorySeparatorChar, '/');
            }

            return "";
        }

        public static void ApplyClassToNodeView(NodeView element){
            if(element.node == null)
                return;
            var nodeType = element.node.GetType();
            CustomNodeAttribute attribute = nodeType.GetCustomAttribute<CustomNodeAttribute>();
            var classToAdd = GetNodeMenuType(attribute, nodeType);
            var elements = classToAdd.Split("/");

            foreach(var className in elements){
                element.AddToClassList(className);
            }
            element.AddToClassList(nodeType.Name);
        }
        
        public static List<VisualElement> CreatePorts<T>(this NodeView viewer, InfiniteLandsNode node, Direction direction, IGraph graph) where T : PropertyAttribute
        {
            var NodeType = node.GetType();
            List<VisualElement> ports = new List<VisualElement>();
            FieldInfo[] fields = NodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            IEnumerable<FieldInfo> inputFields = fields.Where(a => a.GetCustomAttribute<T>() != null);
            foreach (FieldInfo field in inputFields)
            {
                T attribute = field.GetCustomAttribute<T>();
                PortView prt = GraphViewersFactory.CreatePortView(field.FieldType);
                prt.GeneratePorts(ports, viewer, node, field, direction, graph, attribute);
            }
            return ports;
        }
        
        public static Port AddMissingPort(NodeView view, PortData missingData, Direction direction){
            var container = direction == Direction.Input ? view.inputContainer : view.outputContainer;
            CustomMissingPort prt = new CustomMissingPort();
            Port generated = prt.GenerateMissingPort(view, missingData, direction);
            view.ports.Add(generated);
            container.Add(generated);
            view.RefreshExpandedState();
            return generated;
        }

        public static void RemoveMissingPorts(NodeView view)
        {
            var foundPorts = view.ports.Where(a => a.portType == typeof(MISSING)).ToArray();
            foreach (var port in foundPorts)
            {
                view.ports.Remove(port);
                var direction = port.direction;
                var container = direction == Direction.Input ? view.inputContainer : view.outputContainer;
                container.Remove(port);
            }

            if (foundPorts.Length > 0)
            {
                view.RefreshExpandedState();
            }
        }

        public static string GetMissingName(string name)
        {
            return string.Format("[M] {0}", name);
        }

        public static string ClearMissingName(string name){
            if(name.Contains("[M]"))
                return name.Replace("[M] ", "");
            return name;
        }

        public static object GetValueDynamic(object field_holder, string field_name){
            var type = field_holder.GetType();
            FieldInfo field = type.GetField(field_name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if(field != null)
                return field.GetValue(field_holder);  
            
            PropertyInfo property = type.GetProperty(field_name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);            
            if(property != null)
                return property.GetValue(field_holder);  
            return default;
        }

        public static Type GetTypeFromInputField(string lookingForField, InfiniteLandsNode fromNode, IGraph graph){
            var targetInputField = fromNode.GetType().GetField(lookingForField);
            if(targetInputField.FieldType==typeof(object)){
                var connectionToInput = graph.GetAllEdges().FirstOrDefault(a => a.inputPort.fieldName==lookingForField && a.inputPort.nodeGuid==fromNode.guid);
                if(connectionToInput != null)
                    return GetTypeFromOutputField(connectionToInput.outputPort.fieldName, graph.GetNodeFromGUID(connectionToInput.outputPort.nodeGuid), graph);  
                return targetInputField.FieldType;
            }
            else{
                return targetInputField.FieldType;
            }
        }

        public static Type GetTypeFromOutputField(string lookingForField, InfiniteLandsNode fromNode, IGraph graph){
            if(fromNode == null)
                return typeof(object);
            var targetOutputField = fromNode.GetType().GetField(lookingForField);
            if(targetOutputField == null)
                return typeof(object);
                
            var attribute = targetOutputField.GetCustomAttributes().OfType<IMatchInputType>().FirstOrDefault();
            if(targetOutputField.FieldType == typeof(object) && attribute != null){
                if(attribute.matchingType != "")
                    return GetTypeFromInputField(attribute.matchingType, fromNode, graph);
                return targetOutputField.FieldType;
            }
            else{
                return targetOutputField.FieldType;
            }
        }

        public static VisualElement CreateSeparator(){
            VisualElement separator = new VisualElement();
            separator.name = "divider";
            separator.AddToClassList("horizontal");
            return separator;
        }

        public static VisualElement SideBySide(VisualElement left, VisualElement right){
            VisualElement container = new VisualElement();
            container.Add(left);
            container.Add(right);
            container.style.flexDirection = FlexDirection.Row;
            return container;
        }
        
        public static SerializedProperty GetConditionSerializedProperty(SerializedProperty property, string conditionName)
        {
            var path = property.propertyPath.Split('.');
            var prop = property.serializedObject.FindProperty(path[0]);
            
            for (int i = 1; i < path.Length - 1; i++)
            {
                if (path[i] == "Array")
                {
                    i++;
                    int index = int.Parse(path[i].Substring(path[i].IndexOf("[") + 1).TrimEnd(']'));
                    prop = prop.GetArrayElementAtIndex(index);
                }
                else
                {
                    prop = prop.FindPropertyRelative(path[i]);
                }
                if (prop == null) return null;
            }
            return prop?.FindPropertyRelative(conditionName);
        }

        public static object GetFieldContainer(SerializedProperty property, object target)
        {
            try
            {
                var path = property.propertyPath.Split('.');
                object obj = target;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    if (path[i] == "Array")
                    {
                        i++;
                        int index = int.Parse(path[i].Substring(path[i].IndexOf("[") + 1).TrimEnd(']'));
                        obj = (obj as System.Collections.IList)?[index];
                    }
                    else
                    {
                        var f = obj.GetType().GetField(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        obj = f != null ? f.GetValue(obj) : 
                              obj.GetType().GetProperty(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj);
                    }
                    if (obj == null) return null;
                }
                return obj;
            }
            catch { return null; }
        }
    }
}