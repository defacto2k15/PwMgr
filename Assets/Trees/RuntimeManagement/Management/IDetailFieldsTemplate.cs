using System.Collections.Generic;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.Management
{
    public interface IDetailFieldsTemplate
    {
        List<VegetationManagementArea> CalculateManagementAreas(Vector2 center, Vector2 positionDelta);
        List<VegetationManagementArea> CalculateInitialManagementAreas(Vector2 center);
    }
}