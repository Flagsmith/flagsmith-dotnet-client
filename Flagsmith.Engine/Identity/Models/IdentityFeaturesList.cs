using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Feature.Models;
using System.Linq;
using FlagsmithEngine.Exceptions;
namespace FlagsmithEngine.Identity.Models
{
    public class IdentityFeaturesList : List<FeatureStateModel>
    {
        public new void Add(FeatureStateModel model)
        {
            if (this.Any(m => m.Feature.Id == model.Feature.Id))
                throw new DuplicateFeatureState();
            base.Add(model);
        }
    }
}
