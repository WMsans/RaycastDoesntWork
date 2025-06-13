using System;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    public readonly struct ItemInfo{
        public readonly Type type;
        public readonly GroupView groupView;
        public readonly Vector2 mousePosition;
        public ItemInfo(Type type, GroupView groupView, Vector2 mousePosition){
            this.type = type;
            this.groupView = groupView;
            this.mousePosition = mousePosition;
        }
    }
}