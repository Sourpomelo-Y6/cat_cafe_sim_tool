using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    // Observe rendered business UI, never a texture getter or the title/replay gallery.
    [DefaultExecutionOrder(10000)]
    public sealed class ViewedCatImages : MonoBehaviour
    {
        LocalCafeBackend owner;CatGalleryImage[] catalog;readonly HashSet<string> known=new HashSet<string>(),pending=new HashSet<string>();float nextScan,nextWrite;
        IEnumerator Start()
        {
            while(true)
            {
                // Depth and culling must describe the frame actually rendered, not the previous layout.
                yield return new WaitForEndOfFrame();
                if(!isActiveAndEnabled || Time.unscaledTime<nextScan)continue;nextScan=Time.unscaledTime+.25f;Scan();
            }
        }
        public void Scan()
        {
            if(!Application.isPlaying || !CafeBackends.Standalone)return;var b=CafeBackends.Local as LocalCafeBackend;if(b==null)return;
            if(owner!=b){owner=b;known.Clear();pending.Clear();known.UnionWith(b.Gallery.Load().viewed);catalog=CatImageGalleryCatalog.Load();}
            var images=GetComponentsInChildren<RawImage>(true);
            foreach(var definition in catalog)
            {
                if(known.Contains(definition.key))continue;var texture=definition.Texture();if(texture==null)continue;
                if(images.Any(g=>g.texture==texture && IsVisible(g)))pending.Add(definition.key);
            }
            if(pending.Count==0 || Time.unscaledTime<nextWrite)return;
            nextWrite=Time.unscaledTime+5;if(owner.Gallery.Merge(pending)){known.UnionWith(pending);pending.Clear();nextWrite=0;}
        }
        static float Alpha(Graphic g)
        {
            float alpha=g.color.a*g.canvasRenderer.GetAlpha();foreach(var group in g.GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;return alpha;
        }
        static bool Drawable(Graphic g)=>g!=null && g.isActiveAndEnabled && g.canvas!=null && g.canvas.isActiveAndEnabled && !g.canvasRenderer.cull && g.canvasRenderer.absoluteDepth>=0 && Alpha(g)>.05f;
        static bool Inside(Graphic g,Vector2 point,Camera camera)
        {
            if(!RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform,point,camera))return false;
            foreach(var mask in g.GetComponentsInParent<RectMask2D>())if(mask.isActiveAndEnabled && !RectTransformUtility.RectangleContainsScreenPoint(mask.rectTransform,point,camera))return false;
            foreach(var mask in g.GetComponentsInParent<Mask>())if(mask.isActiveAndEnabled && !RectTransformUtility.RectangleContainsScreenPoint(mask.rectTransform,point,camera))return false;
            return true;
        }
        public static bool IsVisible(RawImage image)
        {
            if(!Drawable(image) || image.texture==null)return false;var canvas=image.canvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var graphics=GraphicRegistry.GetGraphicsForCanvas(canvas);var r=image.rectTransform.rect;
            foreach(var sample in new[]{new Vector2(.5f,.5f),new Vector2(.25f,.25f),new Vector2(.75f,.25f),new Vector2(.25f,.75f),new Vector2(.75f,.75f)})
            {
                var world=image.rectTransform.TransformPoint(new Vector3(r.xMin+r.width*sample.x,r.yMin+r.height*sample.y));var point=RectTransformUtility.WorldToScreenPoint(camera,world);
                if(point.x<0 || point.y<0 || point.x>=Screen.width || point.y>=Screen.height || !Inside(image,point,camera))continue;
                bool covered=false;
                for(int index=0;index<graphics.Count;index++)
                {
                    var g=graphics[index];
                    if(g==image || !Drawable(g) || g.canvasRenderer.absoluteDepth<=image.canvasRenderer.absoluteDepth || Alpha(g)<.95f || !(g is Image || g is RawImage || g is ConversationShape || g is ConversationRoomGraphic))continue;
                    if(Inside(g,point,camera)){covered=true;break;}
                }
                if(!covered)return true;
            }
            return false;
        }
    }
}
