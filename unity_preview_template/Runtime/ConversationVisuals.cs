using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatCafePreview
{
    // Small UI primitives, not new bitmap assets. They scale with the full-screen canvas.
    [RequireComponent(typeof(CanvasRenderer))]
    public class ConversationShape : MaskableGraphic
    {
        public float radius;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();float corner=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
            mesh.AddVert(r.center,color,Vector2.zero);int count=0;
            for(int side=0;side<4;side++)
            {
                var center=new Vector2(side==0 || side==3?r.xMax-corner:r.xMin+corner,side<2?r.yMax-corner:r.yMin+corner);
                for(int step=0;step<=8;step++)
                {
                    float angle=(side*90+step*90f/8)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*corner,color,Vector2.zero);count++;
                }
            }
            for(int i=1;i<=count;i++)mesh.AddTriangle(0,i,i==count?1:i+1);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class ConversationRoomGraphic : MaskableGraphic
    {
        VertexHelper mesh;
        protected override void OnPopulateMesh(VertexHelper value)
        {
            mesh=value;mesh.Clear();
            Quad(0,0,1280,800,new Color(.92f,.88f,.79f));
            Quad(0,0,1280,266,new Color(.73f,.63f,.51f));
            Quad(0,260,1280,12,new Color(.65f,.55f,.44f));
            Quad(106,386,386,302,new Color(.72f,.69f,.59f));
            Quad(116,396,366,282,new Color(.97f,.94f,.84f));
            Quad(128,408,342,258,new Color(.85f,.89f,.79f));
            Quad(128,408,160,258,new Color(.93f,.94f,.83f));
            Quad(292,408,7,258,new Color(.98f,.96f,.88f));
            Quad(128,518,342,7,new Color(.98f,.96f,.88f));
            Quad(96,376,406,12,new Color(.65f,.57f,.45f));
            Quad(904,452,268,9,new Color(.63f,.54f,.42f));
            Quad(904,560,268,9,new Color(.63f,.54f,.42f));
            for(int i=0;i<7;i++)Quad(920+i*32,461,19,62+i%3*11,new Color(.67f+i%2*.05f,.69f,.58f));
            Quad(1016,284,72,66,new Color(.65f,.51f,.39f));
            Oval(1050,394,32,74,new Color(.43f,.53f,.39f));
            Oval(1018,374,34,40,new Color(.49f,.58f,.43f));
            Oval(1085,389,27,49,new Color(.53f,.61f,.47f));
            Quad(202,229,18,90,new Color(.53f,.44f,.35f));
            Quad(421,229,18,90,new Color(.53f,.44f,.35f));
            Oval(316,330,224,38,new Color(.58f,.48f,.37f));
            Oval(316,339,224,36,new Color(.70f,.58f,.45f));
        }
        Vector2 Point(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x/1280*r.width,r.yMin+y/800*r.height);}
        void Quad(float x,float y,float w,float h,Color tint)
        {
            int n=mesh.currentVertCount;mesh.AddVert(Point(x,y),tint,Vector2.zero);mesh.AddVert(Point(x+w,y),tint,Vector2.zero);mesh.AddVert(Point(x+w,y+h),tint,Vector2.zero);mesh.AddVert(Point(x,y+h),tint,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        void Oval(float x,float y,float rx,float ry,Color tint)
        {
            int n=mesh.currentVertCount;mesh.AddVert(Point(x,y),tint,Vector2.zero);
            for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;mesh.AddVert(Point(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),tint,Vector2.zero);}
            for(int i=0;i<40;i++)mesh.AddTriangle(n,n+1+i,n+1+(i+1)%40);
        }
    }

    public class ConversationAdvanceSurface : MonoBehaviour, IPointerClickHandler
    {
        public AcquisitionConversationPanel conversation;
        public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)conversation.Next();}
    }
}
