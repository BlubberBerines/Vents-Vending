using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Customers;

namespace VentsAndVending.Tests
{
    public sealed class CustomerIdentityTests
    {
        [Test] public void AppearanceIsStableAndVariesAcrossNames()
        {
            var definition=ScriptableObject.CreateInstance<CustomerDefinition>();
            try
            {
                var field=typeof(CustomerDefinition).GetField("_bodyColors",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                field.SetValue(definition,new[]{Color.red,Color.green,Color.blue});
                int original=definition.AppearanceIndex("Mara");
                Assert.AreEqual(original,definition.AppearanceIndex("Mara"));
                Assert.AreEqual(definition.BodyColors[original],definition.AppearanceColor("Mara"));
                var variants=new System.Collections.Generic.HashSet<int>();
                for(int i=0;i<30;i++)variants.Add(definition.AppearanceIndex("Passenger "+i));
                Assert.AreEqual(3,variants.Count);
                StringAssert.Contains("colouring",definition.IdentificationText("Mara"));
            }
            finally{Object.DestroyImmediate(definition);}
        }
        [Test] public void MissingPaletteAndPortraitAreSafe()
        {
            var definition=ScriptableObject.CreateInstance<CustomerDefinition>();
            try{Assert.AreEqual(0,definition.AppearanceIndex(null));Assert.AreEqual(Color.white,definition.AppearanceColor(null));Assert.IsNull(definition.PortraitFor(null));}
            finally{Object.DestroyImmediate(definition);}
        }
    }
}
