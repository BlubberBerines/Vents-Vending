using TMPro;
using UnityEngine;
using VentsAndVending.Customers;
using VentsAndVending.Crafting;
using VentsAndVending.Interaction;
using VentsAndVending.Orders;

namespace VentsAndVending.UI
{
    public sealed class ConversationPanel : ModalScreen
    {
        private Customer _person;
        private TextMeshProUGUI _name,_reply,_relationship;
        private UnityEngine.UI.Image _portrait;
        private RectTransform _samples;
        private void OnEnable()=>Customer.ConversationRequested+=Open;
        private void OnDisable()=>Customer.ConversationRequested-=Open;
        private void Open(Customer person,InteractionController interactor)
        {
            if(interactor!=Interactor || !BeginOpen()){person.EndConversation();return;}
            _person=person;_name.text=person.DisplayName+" / "+person.Definition.SpeciesName;
            _portrait.sprite=person.Definition.PortraitFor(person.DisplayName);_portrait.color=_portrait.sprite!=null?Color.white:person.Definition.AppearanceColor(person.DisplayName);
            var known=OrderManager.Instance?.Customers.Find(person.DisplayName);
            _reply.text=known!=null && known.Recruited?"Good to see you. Book a time on VentNet when I send you a request.":known!=null && known.DislikedProducts.Count>0?"The last product wasn't for me. I'd try something different another time.":"We haven't done business yet. Got something for me to try?";
            Refresh();
        }
        protected override void OnClosing(){if(_person!=null)_person.EndConversation();_person=null;}
        private void Refresh()
        {
            var record=OrderManager.Instance?.Customers.Find(_person.DisplayName);
            _relationship.text=record!=null && record.Recruited?$"REGULAR CUSTOMER / Trust {record.Trust}/100 / Requests arrive on VentNet":"NEW CONTACT / A liked sample earns their custom. Samples cost one finished product.";
            ClearChildren(_samples);bool any=false;
            foreach(var stack in Interactor.Inventory.Model.Slots)
            {
                if(stack.IsEmpty || !(stack.Item is ProductDefinition product))continue;
                any=true;var quality=stack.Quality;
                var button=UiFactory.Button(_samples,"Sample "+product.Id+" "+quality,$"Offer 1 {product.DisplayName} / {quality}",()=>
                {
                    OrderManager.Instance.Customers.OfferSample(_person.Definition,_person.DisplayName,product,quality,Interactor.Inventory.Model,out var reply);
                    _reply.text=reply;Refresh();
                },Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,ButtonStyle.Go);
                bool tried=record!=null && (record.LikedProducts.Contains(product.Id) || record.DislikedProducts.Contains(product.Id) || record.TriedSamples.Contains(product.Id+":"+(int)quality));
                button.interactable=!tried;
                if(tried)button.GetComponentInChildren<TextMeshProUGUI>().text=record.LikedProducts.Contains(product.Id)?product.DisplayName+" / already a favourite":product.DisplayName+" / already sampled";
                UiFactory.Layout(button.gameObject,preferredHeight:52);
            }
            if(!any){var hint=UiFactory.Text(_samples,"No samples","Make a finished product at the Den mixing table, then bring a sample to a passenger.",22);UiFactory.Layout(hint.gameObject,preferredHeight:100);}
        }
        protected override GameObject BuildUi()
        {
            var canvas=UiFactory.CreateCanvas(transform,"Conversation Canvas",14);UiFactory.Dim(canvas.transform,.5f);
            var panel=UiFactory.Panel(canvas.transform,"Conversation");UiFactory.Place(panel.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1100,680));
            _portrait=UiFactory.Panel(panel.transform,"Mugshot",Color.white,0);_portrait.preserveAspect=true;
            UiFactory.Place(_portrait.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-30),new Vector2(150,150));
            _name=UiFactory.Heading(panel.transform,"Name","",29);UiFactory.Place(_name.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(205,-30),new Vector2(850,48));
            _relationship=UiFactory.Text(panel.transform,"Relationship","",19);UiFactory.Place(_relationship.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(205,-92),new Vector2(830,80));
            _reply=UiFactory.Text(panel.transform,"Reply","",24);UiFactory.Place(_reply.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-210),new Vector2(1040,100));
            _samples=UiFactory.Rect("Samples",panel.transform);UiFactory.Place(_samples,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-340),new Vector2(1040,230));UiFactory.VerticalList(_samples,10);
            UiFactory.Button(panel.transform,"Leave Conversation","Talk later",Close,new Vector2(1,0),new Vector2(1,0),new Vector2(-30,24),new Vector2(230,50));
            return canvas.gameObject;
        }
    }
}
