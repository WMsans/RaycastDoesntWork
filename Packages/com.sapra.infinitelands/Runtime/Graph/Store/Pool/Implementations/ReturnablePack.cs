using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class ReturnablePack : IRelease{
        private List<ToReturn> toReturn = new();
        public void Release()
        {
            for (int i = 0; i < toReturn.Count; i++)
            {
                toReturn[i].Release();
            }
            toReturn.Clear();
            GenericPoolLight.Release(this);
        }
        public void Add(ToReturn data){
            toReturn.Add(data);
        }

        public ReturnablePack Reuse(){
            toReturn.Clear();
            return this;
        }
    }
}