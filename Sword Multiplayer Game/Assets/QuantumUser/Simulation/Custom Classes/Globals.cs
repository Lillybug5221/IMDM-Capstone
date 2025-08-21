using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
    public static class Globals {
        public static EntityRef Get(Frame f) {
            foreach (var entry in f.Unsafe.GetComponentBlockIterator<GlobalTag>()) {
                EntityRef e = entry.Entity;
                return e;
            }
            return default;
        }
    }
}
