using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    public class Adapter : SearcherAdapter
    {
        public Adapter(string title) : base(title){}
        public override bool HasDetailsPanel => false;
        public override float InitialSplitterDetailRatio => 0;
/*         public override SearcherItem OnSearchResultsFilter(IEnumerable<SearcherItem> searchResults, string searchQuery)
        {
            if(searchResults != null && searchQuery != null){
                var found = searchResults.FirstOrDefault(a => {
                    return a.Synonyms != null && a.Synonyms.Any(a=>a.Contains(searchQuery));
                });
                return found?? new SearcherItem("");
            }
            return new SearcherItem("Add");
        } */
    }
}