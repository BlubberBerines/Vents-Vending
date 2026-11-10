using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Player;
using VentsAndVending.World;

namespace VentsAndVending.Tests
{
    public sealed class VentEntryTests
    {
        [Test] public void CrawlProbeDetectsMouthBeforeCapsuleHitsHeader()
        {
            var player=new GameObject("Vent entry test player");
            var mouth=new GameObject("Vent entry test trigger");
            try
            {
                player.transform.position=new Vector3(1000,1000,1000);
                var body=player.AddComponent<CharacterController>();body.radius=.3f;
                var crawler=player.AddComponent<VentCrawler>();
                typeof(VentCrawler).GetField("_body",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(crawler,body);
                mouth.transform.position=player.transform.position+new Vector3(.45f,.3f,0);
                var trigger=mouth.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(.1f,.5f,.5f);
                mouth.AddComponent<VentZone>();Physics.SyncTransforms();
                typeof(VentCrawler).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(crawler,null);
                Assert.IsTrue(crawler.InVent,"The old 0.25m probe missed this mouth; the capsule's leading edge needs advance detection.");
            }
            finally{Object.DestroyImmediate(player);Object.DestroyImmediate(mouth);}
        }
    }
}
