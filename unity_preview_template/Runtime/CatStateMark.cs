using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    // Presentation only: no state, commands, or save data are modified.
    public static class CatStateMark
    {
        public static int Resolve(PythonCatState cat)
        {
            if(cat==null || string.IsNullOrEmpty(cat.cat_id) || cat.health_status!="healthy" || cat.activity!="cafe")return 0;
            foreach(float value in new[]{cat.stress,cat.fatigue,cat.affinity})if(float.IsNaN(value)||float.IsInfinity(value))return 0;
            return cat.stress>=60?1:cat.fatigue>=60?2:cat.affinity>=80?3:0;
        }
        public static string Caption(int kind)=>kind==1?"ストレス高め":kind==2?"疲労高め":kind==3?"好感度高め":"";
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CatStateMarkGraphic : MaskableGraphic
    {
        public int kind;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(kind==3)
            {
                var points=new[]{new Vector2(.5f,.16f),new Vector2(.12f,.55f),new Vector2(.12f,.76f),new Vector2(.26f,.87f),new Vector2(.4f,.87f),new Vector2(.5f,.75f),new Vector2(.6f,.87f),new Vector2(.74f,.87f),new Vector2(.88f,.76f),new Vector2(.88f,.55f)};
                for(int i=0;i<points.Length;i++)Triangle(mesh,new Vector2(.5f,.55f),points[i],points[(i+1)%points.Length]);
            }
            else if(kind==1)
            {Line(mesh,.18f,.82f,.48f,.6f);Line(mesh,.48f,.6f,.3f,.44f);Line(mesh,.3f,.44f,.78f,.18f);}
            else if(kind==2)
            {Line(mesh,.18f,.76f,.4f,.44f);Line(mesh,.4f,.44f,.4f,.22f);Line(mesh,.58f,.76f,.8f,.44f);Line(mesh,.8f,.44f,.8f,.22f);}
        }
        Vector2 Point(Vector2 p){var r=rectTransform.rect;return new Vector2(r.xMin+p.x*r.width,r.yMin+p.y*r.height);}
        void Triangle(VertexHelper m,Vector2 a,Vector2 b,Vector2 c){int n=m.currentVertCount;m.AddVert(Point(a),color,Vector2.zero);m.AddVert(Point(b),color,Vector2.zero);m.AddVert(Point(c),color,Vector2.zero);m.AddTriangle(n,n+1,n+2);}
        void Line(VertexHelper m,float x,float y,float xx,float yy){var a=new Vector2(x,y);var b=new Vector2(xx,yy);var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*.045f;Triangle(m,a+p,b+p,b-p);Triangle(m,a+p,b-p,a-p);}
    }

    public sealed class CatStateMarkView : MonoBehaviour
    {
        RawImage portrait;Text caption;Image badge;CatStateMarkGraphic symbol;
        RectTransform health;Vector2 originalPosition,originalSize;bool bound;
        static RectTransform Place(GameObject go,Transform parent,float x,float y,float w,float h)
        {go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        public void Show(PythonCatDetailsPanel owner,PythonCatState cat,ContentPreview preview)
        {
            if(!bound)
            {
                if(owner.stateDetails==null)return;
                health=owner.stateDetails.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>();originalPosition=health.anchoredPosition;originalSize=health.sizeDelta;bound=true;
                var parent=health.parent;
                portrait=new GameObject("State Portrait",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();Place(portrait.gameObject,parent,24,80,96,96);portrait.raycastTarget=false;
                caption=new GameObject("State Caption",typeof(RectTransform),typeof(Text)).GetComponent<Text>();Place(caption.gameObject,parent,24,180,96,52);caption.font=owner.stateDetails.font;caption.fontSize=16;caption.color=new Color(.16f,.21f,.18f);caption.raycastTarget=false;caption.supportRichText=false;
                badge=new GameObject("State Badge",typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(badge.gameObject,parent,88,84,28,28);badge.color=new Color(1,.98f,.93f,.96f);badge.raycastTarget=false;
                symbol=new GameObject("State Symbol",typeof(RectTransform),typeof(CatStateMarkGraphic)).GetComponent<CatStateMarkGraphic>();Place(symbol.gameObject,badge.transform,0,0,28,28);symbol.raycastTarget=false;
            }
            int stateKind=CatStateMark.Resolve(cat);
            var texture=cat==null?null:(CatStateImages.Resolve(cat,stateKind)??CatOutfits.Resolve(cat)??preview.PythonCatTexture(cat));
            bool visible=texture!=null && texture!=ContentPreview.CatPlaceholderTexture();
            portrait.gameObject.SetActive(visible);caption.gameObject.SetActive(visible);
            health.anchoredPosition=visible?new Vector2(128,-80):originalPosition;health.sizeDelta=visible?new Vector2(200,212):originalSize;
            // The viewport and scrollbar use fixed coordinates in the existing layout.
            var scroll=health.GetComponent<ScrollRect>();var viewport=scroll.viewport;viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,visible?168:originalSize.x-32);
            owner.stateDetails.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,visible?168:originalSize.x-32);
            var bar=scroll.verticalScrollbar.GetComponent<RectTransform>();bar.anchoredPosition=new Vector2((visible?200:originalSize.x)-18,-6);
            portrait.texture=visible?texture:null;portrait.enabled=portrait.texture!=null;
            if(portrait.texture!=null){float ratio=(float)portrait.texture.width/portrait.texture.height;portrait.rectTransform.sizeDelta=ratio>=1?new Vector2(96,96/ratio):new Vector2(96*ratio,96);}
            int kind=visible && portrait.texture!=null && portrait.texture!=ContentPreview.CatPlaceholderTexture()?stateKind:0;
            caption.text=CatStateMark.Caption(kind);badge.gameObject.SetActive(kind!=0);symbol.kind=kind;symbol.color=kind==3?new Color(.72f,.18f,.3f):new Color(.4f,.27f,.16f);symbol.SetVerticesDirty();
        }
        public void Hide(){if(portrait!=null)portrait.gameObject.SetActive(false);if(caption!=null)caption.gameObject.SetActive(false);if(badge!=null)badge.gameObject.SetActive(false);}
    }
}
