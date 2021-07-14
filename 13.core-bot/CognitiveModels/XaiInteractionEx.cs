// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace Luis {
    // Extends the partial XaiInteraction class with methods and properties that simplify accessing entities in the luis results
    public partial class XaiInteraction
    {

    //returns relevant data for FeatureImportance-Intent
    public (string selectedFeature, string number, string ordinal, string ordinalInt ) FeatureImportanceParams
        {
            get
            {
                string selectedFeature = Entities?.feature?[0]?[0];
                string number = (Entities?.number?[0]).HasValue ? ((int)(Entities?.number?[0])).ToString(): null;
                string ordinalInt = (Entities?.ordinal?[0]).HasValue ? ((int)(Entities?.ordinal?[0])).ToString(): null;
                string ordinal = "";
                if (Entities?.HighOrdinal?[0] != null)  ordinal="high";
                else ordinal = "low";

                return (selectedFeature, number, ordinal, ordinalInt);
            }
        }


    }
}
