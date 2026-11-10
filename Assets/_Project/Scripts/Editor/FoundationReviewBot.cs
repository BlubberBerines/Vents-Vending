using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using VentsAndVending.Crafting;
using VentsAndVending.Customers;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Orders;
using VentsAndVending.Player;
using VentsAndVending.Saving;
using VentsAndVending.UI;
using VentsAndVending.World;
using Object=UnityEngine.Object;

namespace VentsAndVending.Editor
{
    /// <summary>Opt-in live integration review. Never runs on load; isolates all writes from player saves.</summary>
    public static class FoundationReviewBot
    {
        private static CustomerOrder _order;
        private static int _paid,_startBalance;
        private static InteractionController User=>Object.FindAnyObjectByType<InteractionController>();
        private static Transform Frame=>GameObject.Find("Station_Service_Network").transform;
        private static Transform Room=>GameObject.Find("Ring Room 242 TOOL STORE").transform;
        private static Transform Branch=>GameObject.Find("L3_Corrupted_Core_Recovery").transform;
        private static void Check(bool value,string message)
        {if(!value)throw new InvalidOperationException(message);Directory.CreateDirectory("Logs/FoundationReview");File.AppendAllText("Logs/FoundationReview/live.txt","PASS "+message+"\n");}
        private static Vector3 Ring(float angle)
        {float a=angle*Mathf.Deg2Rad;return new Vector3(72*Mathf.Cos(a),0,72*Mathf.Sin(a));}

        public static async Task<string> BeginCycle(int cycle)
        {
            SaveManager.ReviewDirectory=Path.GetFullPath("Logs/FoundationReview/Saves");
            foreach(var modal in Object.FindObjectsByType<ModalScreen>(FindObjectsSortMode.None))modal.Close();
            var manager=OrderManager.Instance;var station=Object.FindAnyObjectByType<MixingStation>();
            var who=manager.FindCustomer("blorb_socialite");
            _order=manager.Board.AddOffer(new CustomerOrder{CustomerId=who.Id,CustomerName="Foundation Guest "+cycle,ProductId=station.Recipes[0].Output.Id,Quantity=1,MinimumQuality=ItemQuality.Standard,SpotId=manager.Spots[0].SpotId,RoomMeeting=true,Markup=.15f});
            var panel=Object.FindAnyObjectByType<VentNetPanel>();Object.FindAnyObjectByType<VentNetTerminal>().Interact(User);await Task.Delay(120);
            panel.GetComponentsInChildren<Button>().First(b=>b.name=="TabMessages").onClick.Invoke();await Task.Delay(100);
            panel.GetComponentsInChildren<Button>().First(b=>b.name=="Accept" && b.transform.parent.name==_order.Id).onClick.Invoke();await Task.Delay(100);
            Check(_order.Status==OrderStatus.Accepted,"Cycle "+cycle+" accepted through PC");
            panel.GetComponentsInChildren<Button>().First(b=>b.name=="TabSupplies").onClick.Invoke();await Task.Delay(100);
            _startBalance=User.GetComponent<PlayerWallet>().Wallet.Balance;
            foreach(string id in new[]{"glarb_extract","stabiliser_gel"})
            {panel.GetComponentsInChildren<Button>().First(b=>b.name=="AddToCart" && b.transform.parent.name==id).onClick.Invoke();await Task.Delay(100);}
            Check(User.GetComponent<PlayerWallet>().Wallet.Balance==_startBalance,"Draft cart is unpaid");
            panel.GetComponentsInChildren<Button>().First(b=>b.name=="Checkout Cart").onClick.Invoke();await Task.Delay(100);
            _paid=_startBalance-User.GetComponent<PlayerWallet>().Wallet.Balance;
            Check(_paid>0 && SupplyDeliveryService.Instance.Queue.Orders.Count==2,"Combined checkout paid and queued two ingredients");panel.Close();
            Check(SaveManager.Instance.Save(null),"In-flight order saved");
            Check(SaveManager.Instance.Load(),"In-flight order loaded");
            _order=manager.Board.Find(_order.Id);
            Check(SupplyDeliveryService.Instance.Queue.Orders.Count==2,"Load preserved paid manifest without duplication");
            return "Cycle "+cycle+" booked; paid "+_paid;
        }

        public static async Task<string> Walk(params Vector3[] points)
        {
            var move=User.GetComponent<PlayerMovement>();var camera=Object.FindAnyObjectByType<PlayerCamera>();
            var settings=InputSystem.settings;var behavior=settings.backgroundBehavior;
            var property=settings.GetType().GetProperty("editorInputBehaviorInPlayMode");var editorBehavior=property?.GetValue(settings);
            Keyboard keyboard=null;bool enabled=camera.enabled;
            try
            {
                camera.enabled=false;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                if(property!=null)property.SetValue(settings,Enum.Parse(property.PropertyType,"AllDeviceInputAlwaysGoesToGameView"));
                keyboard=InputSystem.AddDevice<Keyboard>();
                var doors=Object.FindObjectsByType<SlidingDoor>(FindObjectsSortMode.None);
                foreach(var target in points)
                {
                    float started=Time.realtimeSinceStartup;var last=move.transform.position;float changed=started;
                    while(true)
                    {
                        var difference=target-move.transform.position;difference.y=0;if(difference.magnitude<.32f)break;
                        if(Time.realtimeSinceStartup-started>35)throw new InvalidOperationException("Walk timeout "+target+" at "+move.transform.position);
                        if(Vector3.Distance(last,move.transform.position)>.12f){last=move.transform.position;changed=Time.realtimeSinceStartup;}
                        if(Time.realtimeSinceStartup-changed>5)throw new InvalidOperationException("Walk blocked toward "+target+" at "+move.transform.position);
                        foreach(var door in doors)if(!door.Locked && Vector3.Distance(door.transform.position,move.transform.position)<2.3f && door.GetPrompt(User)!=null)door.Interact(User);
                        move.transform.rotation=Quaternion.LookRotation(difference);
                        InputState.Change(keyboard,new KeyboardState(Key.W));await Task.Delay(65);
                    }
                    InputState.Change(keyboard,new KeyboardState());await Task.Delay(75);
                }
                Check(true,"Walked "+points.Length+" waypoints with CharacterController");return move.transform.position.ToString();
            }
            finally
            {if(keyboard!=null)InputSystem.RemoveDevice(keyboard);settings.backgroundBehavior=behavior;if(property!=null)property.SetValue(settings,editorBehavior);camera.enabled=enabled;}
        }

        public static Task<string> OutboundRing()=>Walk(new[]{Frame.TransformPoint(new Vector3(0,0,-5)),Frame.TransformPoint(new Vector3(0,0,3)),Frame.TransformPoint(new Vector3(0,0,10))}.Concat(new[]{12.5f,0,345,330,315,300,285,270,255,242}.Select(Ring)).ToArray());
        public static Task<string> IntoCore()=>Walk(new[]{Room.TransformPoint(new Vector3(0,0,-3)),Room.TransformPoint(new Vector3(-1,0,-8)),Room.TransformPoint(new Vector3(-1,0,-12))}.Concat(new[]{new Vector3(10,0,-4),new Vector3(13,0,-4),new Vector3(17,-.9f,-7),new Vector3(17,-1.8f,-13),new Vector3(24,-1.8f,-13),new Vector3(24,-1.8f,-18),new Vector3(22,-1.8f,-19)}.Select(Branch.TransformPoint)).ToArray());
        public static async Task<string> Mine()
        {
            Object.FindAnyObjectByType<MiningToolRack>().Interact(User);
            await Walk(Branch.TransformPoint(new Vector3(21,-1.8f,-21.5f)));
            var vein=Object.FindObjectsByType<CrystalVein>(FindObjectsSortMode.None).First(v=>v.SaveId=="CORE_CRYSTAL_01");
            int before=User.Inventory.Model.Count(Object.FindAnyObjectByType<CrystalRefinery>().Raw);
            for(int i=0;i<3;i++){vein.Interact(User);await Task.Delay(720);}
            Check(User.Inventory.Model.Count(Object.FindAnyObjectByType<CrystalRefinery>().Raw)==before+3,"Three real-time strikes yielded three raw crystals");
            return "Harvested";
        }
        public static Task<string> LeaveCore()=>Walk(new[]{new Vector3(20,-1.8f,-21),new Vector3(20,-1.8f,-18.7f),new Vector3(24,-1.8f,-18.7f),new Vector3(24,-1.8f,-18),new Vector3(24,-1.8f,-13),new Vector3(17,-1.8f,-13),new Vector3(17,-.9f,-7),new Vector3(13,0,-4),new Vector3(10,0,-4)}.Select(Branch.TransformPoint).Concat(new[]{Room.TransformPoint(new Vector3(-1,0,-12)),Room.TransformPoint(new Vector3(-1,0,-8)),Room.TransformPoint(new Vector3(0,0,-3)),Ring(242)}).ToArray());
        public static Task<string> ReturnRing()=>Walk(new[]{255f,270,285,300,315,330,345,0,12.5f}.Select(Ring).ToArray());
        public static async Task<string> CollectAndRefine()
        {
            var service=SupplyDeliveryService.Instance;
            await Walk(Ring(15));
            Check(service.Queue.Orders.Count==0,"Physical courier completed the combined delivery during travel");
            foreach(var modal in Object.FindObjectsByType<ModalScreen>(FindObjectsSortMode.None))modal.Close();
            var inventory=Object.FindAnyObjectByType<InventoryScreen>();inventory.Open(service.DropBox);await Task.Delay(100);
            // Same inventory transfer operation as clicking each crate row.
            for(int i=0;i<service.DropBox.Model.Capacity;i++)service.DropBox.Model.TransferSlotTo(i,User.Inventory.Model);
            inventory.Close();Check(service.DropBox.Model.IsEmpty,"Collected entire delivered crate");
            await Walk(Ring(12.5f),Frame.TransformPoint(new Vector3(0,0,3)),Frame.TransformPoint(new Vector3(0,0,-7.3f)));
            var refinery=Object.FindAnyObjectByType<CrystalRefinery>();refinery.Interact(User);
            Check(refinery.Process.HasBatch,"Raw crystals loaded into refinery");
            Check(SaveManager.Instance.Save(null) && SaveManager.Instance.Load(),"Refinery batch save/load succeeded");_order=OrderManager.Instance.Board.Find(_order.Id);
            await Task.Delay(8300);refinery.Interact(User);Check(!refinery.Process.HasBatch,"Refined output collected after actual eight-second process");
            return "Supplies and refined gel in pockets";
        }
        public static async Task<string> Mix()
        {
            var station=Object.FindAnyObjectByType<MixingStation>();var recipe=station.Recipes[0];station.Interact(User);await Task.Delay(100);
            foreach(var ingredient in recipe.Ingredients)for(int i=0;i<ingredient.Quantity;i++)Check(station.TryLoad(User,ingredient.Item),"Loaded "+ingredient.Item.Id);
            var panel=Object.FindAnyObjectByType<MixingPanel>();panel.GetComponentsInChildren<Button>().First(b=>b.name=="StartButton").onClick.Invoke();
            float end=Time.realtimeSinceStartup+10;
            while(station.Process.Progress<.92f && Time.realtimeSinceStartup<end)await Task.Delay(20);
            station.Interact(User);Check(station.Process.ResultQuality>=ItemQuality.Standard,"Stopped live mixer at sellable quality");
            Check(SaveManager.Instance.Save(null) && SaveManager.Instance.Load(),"Settling mixer batch save/load succeeded");_order=OrderManager.Instance.Board.Find(_order.Id);
            await Task.Delay(2800);station.Interact(User);Check(User.Inventory.Model.Count(recipe.Output)>0,"Collected finished product");
            return station.Process.Phase.ToString();
        }
        public static async Task<string> Handoff()
        {
            var manager=OrderManager.Instance;
            if(_order.Status==OrderStatus.Accepted)manager.Board.Tick(_order.WindowOpensAt-manager.Board.Clock+.01f,new System.Random(9));
            var customer=Object.FindObjectsByType<Customer>(FindObjectsSortMode.None).First(c=>c.Order==_order);
            float deadline=Time.realtimeSinceStartup+45;
            while(customer.State==CustomerState.Arriving && Time.realtimeSinceStartup<deadline)await Task.Delay(100);
            Check(customer.State==CustomerState.Waiting,"Customer navigated from remote arrival to lounge");
            var color=manager.FindCustomer(_order.CustomerId).AppearanceColor(_order.CustomerName);
            Check(manager.FindCustomer(_order.CustomerId).PortraitFor(_order.CustomerName)!=null,"Customer has matching named portrait variant");
            // Isolate sale logic from public-route traversal (covered by separate movement review).
            var movement=User.GetComponent<PlayerMovement>();var old=movement.transform.position;var rotation=movement.transform.rotation;
            movement.Teleport(customer.transform.position+customer.transform.forward*1.2f,customer.transform.rotation*Quaternion.Euler(0,180,0));
            customer.Interact(User);await Task.Delay(150);var sale=Object.FindAnyObjectByType<SalePanel>();Check(sale.IsOpen,"Customer sale panel opened");
            int balance=User.GetComponent<PlayerWallet>().Wallet.Balance;int stock=User.Inventory.Model.Count(customer.WantedProduct);int trust=manager.Board.Trust;
            var button=sale.GetComponentsInChildren<Button>().First(b=>b.name=="OfferButton");Check(button.interactable,"Hand over enabled with crafted stock");button.onClick.Invoke();
            Check(_order.Status==OrderStatus.Completed && User.GetComponent<PlayerWallet>().Wallet.Balance==balance+_order.Payout,"Handoff paid exactly the recorded amount");
            Check(User.Inventory.Model.Count(customer.WantedProduct)==stock-1 && manager.Board.Trust==Math.Min(100,trust+OrderRules.TrustForCompleting),"Handoff removed exact stock and awarded trust once");
            Check(SaveManager.Instance.Save(null) && SaveManager.Instance.Load(),"Completed loop saved and reloaded");
            Check(manager.Board.Find(_order.Id).Status==OrderStatus.Completed,"Completed order did not respawn on load");
            movement.Teleport(old,rotation);return "Completed / payout "+_order.Payout;
        }
    }
}
