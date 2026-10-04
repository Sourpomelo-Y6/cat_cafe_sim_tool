using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonRosterSetup
    {
        static Font Font { get { return ContentPreviewLayout.LoadFont(); } }
        static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var text = Box(parent, name, x,y,w,h).gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.text = value; text.color = new Color(.16f,.21f,.18f);
            text.raycastTarget = false; text.supportRichText = false; return text;
        }
        static Button Button(Transform parent, string name, string value, float x)
        {
            var rect = Box(parent,name,x,230,106,30); rect.gameObject.AddComponent<Image>().color = new Color(.9f,.93f,.9f);
            var button = rect.gameObject.AddComponent<Button>(); button.interactable = false;
            var label = Label(rect,name+" Label",value,0,0,106,30,16); label.alignment = TextAnchor.MiddleCenter; return button;
        }
        static Sprite CustomerSprite()
        {
            const string path = "Assets/CatCafePreview/CustomerIcon.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(96,96,TextureFormat.RGBA32,false);
                for (int y=0;y<96;y++) for(int x=0;x<96;x++)
                {
                    bool head = (x-48)*(x-48)+(y-65)*(y-65)<17*17;
                    bool body = y>=12 && y<=43 && (x-48)*(x-48)/900f+(y-16)*(y-16)/900f<1;
                    texture.SetPixel(x,y,head||body ? new Color(.22f,.39f,.34f,1) : Color.clear);
                }
                texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static void BuildAndVerify()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var root = preview.transform.Find("Content Canvas/Background");
            if(root == null) throw new Exception("Preview background missing");
            root.Find("Image Panel").gameObject.SetActive(false);
            root.Find("Cat Selection").gameObject.SetActive(false);
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                if(text.text == "猫カフェ 素材プレビュー") { text.text = "猫カフェ 営業画面"; PrefabUtility.RecordPrefabInstancePropertyModifications(text); }
            var heading = root.Find("Roster Heading"); if(heading == null) Label(root,"Roster Heading","猫一覧　　人物アイコン：接客中のお客さん",24,76,750,38,20);
            preview.GetSceneButton(0).gameObject.SetActive(false);
            preview.GetSceneButton(1).gameObject.SetActive(false);
            var business = preview.GetSceneButton(2).GetComponent<RectTransform>(); business.anchoredPosition = new Vector2(24,-128);
            var save = preview.GetSceneButton(3).GetComponent<RectTransform>(); save.anchoredPosition = new Vector2(204,-128);
            // Keep scene and prefab editable; build the layout only in the editor.
            var previous = root.Find("Cat Roster Scroll"); if(previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var scrollRect = Box(root,"Cat Roster Scroll",24,188,750,550);
            scrollRect.gameObject.AddComponent<Image>().color = new Color(.89f,.9f,.86f);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Box(scrollRect,"Viewport",0,0,750,550); viewport.gameObject.AddComponent<Image>();
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Box(viewport,"Cat Cards",0,0,750,550);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(242,266);
            grid.spacing = new Vector2(8,8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content; scroll.movementType = ScrollRect.MovementType.Clamped;
            var card = Box(null,"Cat Card",0,0,242,266); card.gameObject.AddComponent<Image>().color = Color.white;
            var view = card.gameObject.AddComponent<PythonCatCard>();
            view.catName = Label(card,"Cat Name","猫",10,0,175,42,20);
            var image = Box(card,"Cat Image",46,36,150,150).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false; view.picture = image;
            view.stateText = Label(card,"State","接続待ち",10,190,222,38,16);
            var customer = Box(card,"Customer Icon",198,6,34,34).gameObject.AddComponent<Image>();
            customer.sprite = CustomerSprite(); customer.raycastTarget = false; customer.preserveAspect = true;
            view.customerIcon = customer.gameObject; customer.gameObject.SetActive(false);
            view.work = Button(card,"Work","出勤",10); view.rest = Button(card,"Rest","休養",126);
            const string prefabPath = "Assets/CatCafePreview/Prefabs/PythonCatCard.prefab";
            PrefabUtility.SaveAsPrefabAsset(card.gameObject,prefabPath); UnityEngine.Object.DestroyImmediate(card.gameObject);
            var roster = preview.GetComponent<PythonCatRoster>() ?? preview.gameObject.AddComponent<PythonCatRoster>();
            roster.content = content; roster.cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<PythonCatCard>();
            preview.Initialize();
            var ids = new[]{"mike","tama","sora","kohaku","mugi"}; var names = new[]{"ミケ","タマ","ソラ","コハク","ムギ"};
            var state = new PythonCafeState { version=1, cats=new PythonCatState[5], seats=new PythonSeatState[0] };
            for(int i=0;i<5;i++) state.cats[i] = new PythonCatState {cat_id="cat-"+ids[i],content_cat_id="playtest-"+ids[i],name=names[i],working=true,health_status="healthy",activity="cafe",stamina=100,max_stamina=100};
            roster.Apply(state);
            if(roster.Count != 5) throw new Exception("Roster cat count mismatch");
            state.seats = new[]{new PythonSeatState{cat_id="cat-mike",customer_id="guest-1"},new PythonSeatState{cat_id="cat-tama",customer_id="guest-2"}};
            roster.Apply(state); if(roster.CustomerCount != 2) throw new Exception("Customer icons mismatch");
            state.seats = new PythonSeatState[0]; roster.Apply(state);
            if(roster.CustomerCount != 0) throw new Exception("Stale customer icon");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_ROSTER_OK: 5 visible cats / shared customer icon / seat matching / departed customer cleared");
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var roster = preview.GetComponent<PythonCatRoster>();
            var cards = roster.content.GetComponentsInChildren<PythonCatCard>();
            cards[0].customerIcon.SetActive(true); cards[0].stateText.text = "接客中　体力 85/100";
            cards[1].customerIcon.SetActive(true); cards[1].stateText.text = "接客中　体力 92/100";
            Canvas.ForceUpdateCanvases();
            var capture = typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            capture.Invoke(null,new object[]{preview,"Logs/CatRoster.png"});
            Debug.Log("CAT_CAFE_ROSTER_CAPTURE_OK");
        }
        public static void BuildAndCapture() { BuildAndVerify(); Capture(); }
    }
}
