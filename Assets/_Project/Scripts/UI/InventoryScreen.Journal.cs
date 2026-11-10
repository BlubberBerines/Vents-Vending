using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Orders;

namespace VentsAndVending.UI
{
    public sealed partial class InventoryScreen
    {
        private GameObject _pocketColumn,_dealsPage,_contactsPage;
        private RectTransform _dealsList,_contactsList;
        private Button _inventoryTab,_dealsTab,_contactsTab;
        private TextMeshProUGUI _journalClock;
        private int _page;
        private bool _journalAvailable,_journalDirty;
        private float _journalNextRefresh;
        private OrderManager _journalManager;
        private readonly List<(TextMeshProUGUI label,CustomerOrder order)> _journalTimes=new();
        private void BuildJournal(Transform panel)
        {
            _inventoryTab=UiFactory.Button(panel,"Inventory Tab","Inventory",()=>ShowJournalPage(0),new Vector2(0,1),new Vector2(0,1),new Vector2(30,-24),new Vector2(180,48));
            _dealsTab=UiFactory.Button(panel,"Deals Tab","In-progress deals",()=>ShowJournalPage(1),new Vector2(0,1),new Vector2(0,1),new Vector2(226,-24),new Vector2(280,48));
            _contactsTab=UiFactory.Button(panel,"Customers Tab","Customers",()=>ShowJournalPage(2),new Vector2(0,1),new Vector2(0,1),new Vector2(522,-24),new Vector2(210,48));
            _journalClock=UiFactory.Mono(panel,"Station Clock","",20,TextAlignmentOptions.Right);
            UiFactory.Place(_journalClock.rectTransform,new Vector2(1,1),new Vector2(1,1),new Vector2(-30,-30),new Vector2(370,36));
            _dealsPage=JournalPage(panel,"Deal Book",out _dealsList);_contactsPage=JournalPage(panel,"Customer Database",out _contactsList);
            _dealsPage.SetActive(false);_contactsPage.SetActive(false);
        }
        private static GameObject JournalPage(Transform parent,string name,out RectTransform content)
        {
            var viewport=UiFactory.Rect(name,parent);UiFactory.Place(viewport,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-98),new Vector2(1120,550));
            viewport.gameObject.AddComponent<RectMask2D>();var background=viewport.gameObject.AddComponent<Image>();background.color=Color.clear;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            content=UiFactory.Rect("Entries",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            UiFactory.VerticalList(content,12);content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;return viewport.gameObject;
        }
        private void OpenJournal(bool available)
        {
            _journalAvailable=available;_journalManager=OrderManager.Instance;
            if(_journalManager!=null){_journalManager.Board.Changed+=DirtyJournal;_journalManager.Customers.Changed+=DirtyJournal;}
            _inventoryTab.gameObject.SetActive(available);_dealsTab.gameObject.SetActive(available);_contactsTab.gameObject.SetActive(available);
            ShowJournalPage(available?_page:0);
        }
        private void CloseJournal()
        {
            if(_journalManager!=null){_journalManager.Board.Changed-=DirtyJournal;_journalManager.Customers.Changed-=DirtyJournal;}
            _journalManager=null;
        }
        private void DirtyJournal()=>_journalDirty=true;
        protected override void UpdateWhileOpen()
        {
            if(_journalManager==null)return;
            if(_journalDirty){_journalDirty=false;RefreshJournal();}
            if(Time.unscaledTime<_journalNextRefresh)return;_journalNextRefresh=Time.unscaledTime+.25f;
            _journalClock.text=StationTime.Label(_journalManager.Board.Clock)+(StationTime.IsNight(_journalManager.Board.Clock)?" / NIGHT":" / DAY");
            foreach(var entry in _journalTimes)if(entry.label!=null)entry.label.text=MeetingText(entry.order,_journalManager.Board.Clock);
        }
        private void ShowJournalPage(int page)
        {
            if(_journalAvailable)_page=page;
            _pocketColumn.SetActive(page==0);_otherColumn.SetActive(page==0 && _other!=null);_details.gameObject.SetActive(page==0);
            _dealsPage.SetActive(page==1);_contactsPage.SetActive(page==2);
            _inventoryTab.interactable=page!=0;_dealsTab.interactable=page!=1;_contactsTab.interactable=page!=2;
            if(page!=0)_hint.text=page==1?"Track one deal for a compact HUD reminder. Full meeting details stay here.":"Talk to passengers. A liked sample recruits a regular; other tastes can be explored later.";
            else _hint.text=_other!=null?"Click an item to transfer / Tab or Esc closes":"Click an item to drop it / Tab or Esc closes";
            RefreshJournal();
        }
        private static string MeetingText(CustomerOrder order,float now)=>
            $"Meet: {StationTime.Label(order.WindowOpensAt)} / {(order.Status==OrderStatus.Waiting?$"HERE NOW — {OrderRules.Clock(order.WindowClosesAt-now)} left":"scheduled")}\nWait window: 2m 30s real gameplay time";
        private void RefreshJournal()
        {
            ClearChildren(_dealsList);ClearChildren(_contactsList);_journalTimes.Clear();
            if(_journalManager==null)return;
            int deals=0;
            foreach(var order in _journalManager.Board.Orders)
            {
                if(!order.IsActive)continue;deals++;
                var card=UiFactory.Panel(_dealsList,order.Id);UiFactory.Layout(card.gameObject,preferredHeight:190);
                Mugshot(card.transform,order.CustomerId,order.CustomerName);
                var product=_journalManager.FindProduct(order.ProductId);var spot=_journalManager.FindSpot(order.SpotId);
                var title=UiFactory.Heading(card.transform,"Customer",order.CustomerName,25);PlaceJournal(title,new Vector2(140,-15),new Vector2(650,34));
                var description=UiFactory.Text(card.transform,"Order",$"{order.Quantity} x {product?.DisplayName??order.ProductId} / {order.MinimumQuality}+\nLocation: {spot?.Label??order.SpotId}",20);PlaceJournal(description,new Vector2(140,-54),new Vector2(690,58));
                var time=UiFactory.Text(card.transform,"Meeting",MeetingText(order,_journalManager.Board.Clock),18);PlaceJournal(time,new Vector2(140,-119),new Vector2(700,58));_journalTimes.Add((time,order));
                bool tracked=_journalManager.TrackedOrderId==order.Id;
                UiFactory.Button(card.transform,"Track "+order.Id,tracked?"Untrack":"Track deal",()=>{_journalManager.Track(tracked?null:order.Id);RefreshJournal();},new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-18,0),new Vector2(200,52),tracked?ButtonStyle.Primary:ButtonStyle.Go);
            }
            if(deals==0)JournalNote(_dealsList,"No booked deals. Read requests and choose a meeting time on the Den PC.");
            int contacts=0;
            foreach(var record in _journalManager.Customers.Records)
            {
                contacts++;var card=UiFactory.Panel(_contactsList,"Contact "+record.Name);UiFactory.Layout(card.gameObject,preferredHeight:164);Mugshot(card.transform,record.DefinitionId,record.Name);
                var title=UiFactory.Heading(card.transform,"Name",record.Name,25);PlaceJournal(title,new Vector2(140,-16),new Vector2(940,36));
                string liked=string.Join(", ",record.LikedProducts.ConvertAll(id=>_journalManager.FindProduct(id)?.DisplayName??id));
                string disliked=string.Join(", ",record.DislikedProducts.ConvertAll(id=>_journalManager.FindProduct(id)?.DisplayName??id));
                var detail=UiFactory.Text(card.transform,"Trust",$"{(record.Recruited?"REGULAR CUSTOMER":"CONTACT / not recruited")} / Trust {record.Trust}/100\nLikes: {(liked.Length>0?liked:"not discovered")}\nNot keen on: {(disliked.Length>0?disliked:"not discovered")}",19);PlaceJournal(detail,new Vector2(140,-60),new Vector2(940,90));
            }
            if(contacts==0)JournalNote(_contactsList,"Nobody knows your product yet. Talk to passengers in the lounge and offer a finished sample.");
        }
        private void Mugshot(Transform parent,string id,string name)
        {
            var definition=_journalManager.FindCustomer(id);var image=UiFactory.Panel(parent,"Mugshot",Color.white,0);image.sprite=definition?.PortraitFor(name);image.preserveAspect=true;
            UiFactory.Place(image.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-18),new Vector2(104,104));
        }
        private static void PlaceJournal(TextMeshProUGUI text,Vector2 position,Vector2 size)=>UiFactory.Place(text.rectTransform,new Vector2(0,1),new Vector2(0,1),position,size);
        private static void JournalNote(Transform parent,string message){var text=UiFactory.Text(parent,"Empty",message,23);UiFactory.Layout(text.gameObject,preferredHeight:100);}
    }
}
