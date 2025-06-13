using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    public class ContextualMenuBuilder
    {
        private IGraph graph;
        private TerrainGeneratorView view;
        public ContextualMenuBuilder(TerrainGeneratorView view){
            this.view = view;
            this.graph = view.targetGraph;
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            GroupView actualGroup = view.Query<GroupView>().Where((GroupView a) => 
                a.ContainsPoint(a.WorldToLocal(evt.mousePosition))).First();
            
            evt.menu.AppendAction("Create Node _N", a => CreateNodeWindow(view.MousePosition));
            evt.menu.AppendAction("Create StickyNote _S", a => CreateStickyNote(view.MousePosition));
            if(actualGroup == null){
                evt.menu.AppendAction("Create Group _G", a => CreateGroup(view.MousePosition));
            }
            if(EditorTools.DebugMode){
                evt.menu.AppendAction("Create All Nodes", a => CreateAllNodes());
            }
            evt.menu.AppendSeparator();
        }
        
        private List<SearcherItem> GetSearchItems(GroupView ogGroup, Vector2 position){
            List<SearcherItem> items = new List<SearcherItem>();
            var types = TypeCache.GetTypesDerivedFrom<InfiniteLandsNode>();
/*             var sortedTypes = types.Where(a => RuntimeTools.GetAttribute<CustomNodeAttribute>(a) != null).OrderBy(b => {
                CustomNodeAttribute attributeA = RuntimeTools.GetAttribute<CustomNodeAttribute>(b);
                return attributeA.name;
            });
 */
            foreach (var type in types)
            {
                CustomNodeAttribute attribute = type.GetCustomAttribute<CustomNodeAttribute>();
                bool validNode = attribute != null && attribute.canCreate && attribute.IsValidInTree(graph.GetType());
                int existingNodes = graph.GetBaseNodes().Count(a => a.GetType().Equals(type));
                bool canCreate = attribute != null && (!attribute.singleInstance || (attribute.singleInstance && existingNodes == 0));

                if (validNode && canCreate){
                    string nodeType = EditorTools.GetNodeMenuType(attribute, type);
                    SearcherItem nodeItem = new SearcherItem(nodeType+"/"+attribute.name)
                    {
                        UserData = new ItemInfo(type, ogGroup, position),
                        Synonyms = attribute.synonims
                    };
                    items.Add(nodeItem);
                }
            }
            items.Sort((a,b) => a.Name.CompareTo(b.Name));
            return CustomSearchTreeUtility.CreateFromFlatList(items);
        }

        public void CreateNodeWindow(Vector2 position){
            GroupView groupView = view.Query<GroupView>().Where((GroupView a) => 
                a.ContainsPoint(a.WorldToLocal(position))).First();
            Adapter adapter = new Adapter("Create Node");
            SearcherWindow.Show(EditorWindow.focusedWindow, GetSearchItems(groupView,position), adapter, CreateNode, position, default);
        }

        public void CreateAllNodes()
        {
            var types = TypeCache.GetTypesDerivedFrom<InfiniteLandsNode>();
            foreach (var nodeType in types)
            {
                CustomNodeAttribute attribute = nodeType.GetCustomAttribute<CustomNodeAttribute>();
                bool validNode = attribute != null && attribute.canCreate && attribute.IsValidInTree(graph.GetType());
                if (validNode)
                {
                    InfiniteLandsNode nd = graph.CreateNode(nodeType, Vector2.zero);
                    NodeView nodeView = GraphViewersFactory.CreateNodeView(view, nd);
                    view.AddNode(nodeView);
                }
            }
        }
        
        private bool CreateNode(SearcherItem item){
            if(item == null)
                return false;
            ItemInfo data = (ItemInfo)item.UserData;
            InfiniteLandsNode nd = graph.CreateNode(data.type,
                view.viewTransform.matrix.inverse.MultiplyPoint(data.mousePosition));
            
            NodeView nodeView = GraphViewersFactory.CreateNodeView(view, nd);
            view.AddNode(nodeView);
            if(data.groupView != null)
                data.groupView.AddElement(nodeView);
            return true;
        }

        public void CreateStickyNote(Vector2 position){
            GroupView actualGroup = view.Query<GroupView>().Where((GroupView a) => 
                a.ContainsPoint(a.WorldToLocal(position))).First();

            
            StickyNoteBlock note = graph.CreateStickyNote(view.viewTransform.matrix.inverse.MultiplyPoint(position));
            StickyNoteView noteView  = GraphViewersFactory.CreateStickyNoteView(note);
            view.AddStickyNoteView(noteView);
            if(actualGroup != null){
                actualGroup.AddElement(noteView);
            }
        }

        public void CreateGroup(Vector2 position){
            GroupView actualGroup = view.Query<GroupView>().Where((GroupView a) => 
                a.ContainsPoint(a.WorldToLocal(position))).First();
            if(actualGroup != null)
                return;

            List<GraphElement> elements = view.selection.OfType<GraphElement>().Where(a => a.IsGroupable()).ToList();
            List<string> elementsGuids = elements.Select(a => a.viewDataKey).ToList();

            GroupBlock block = graph.CreateGroup("Name", view.viewTransform.matrix.inverse.MultiplyPoint(position), elementsGuids);
            GroupView blockView = GraphViewersFactory.CreateGroupView(block, elements);
            view.AddGroupView(blockView);
        }
    }
}