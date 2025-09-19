using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
    public class DefaultCancelRulesAsset : AssetObject
    {
        public CancelRule[] Rules;
    }

    /*
        initialize cancelabilit lists at runtime like this
        var combinedRules = new Dictionary<string, CancelRule>();

        // 1. Add defaults first
        if (!actionConfig.DefaultRules.IsEmpty) {
            var defaults = frame.FindAsset<DefaultCancelRulesAsset>(actionConfig.DefaultRules);
            foreach (var rule in defaults.Rules) {
                combinedRules[rule.TargetAction] = rule; // default rule
            }
        }

        // 2. Add/replace custom rules
        if (actionConfig.CustomCancelRules != null) {
            foreach (var custom in actionConfig.CustomCancelRules) {
                combinedRules[custom.TargetAction] = custom; // overrides default
            }
        }
            */
}
