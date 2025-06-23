using System.Collections.Generic;
using sapra.InfiniteLands;
using UnityEngine;

[CustomNode("Set Fog Mask")]
public class SetFogMaskNode : InfiniteLandsNode
{
    [Input] public PointInstance maskCenters;
    protected override void Process(BranchData branch)
    {
        // TODO: Use FogMaskSettings to set the mask
        
    }
}
