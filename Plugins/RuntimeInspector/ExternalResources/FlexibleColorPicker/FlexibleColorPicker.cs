using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlexibleColorPicker : MonoBehaviour {

    /*----------------------------------------------------------
    * ----------------------- PARAMETERS -----------------------
    * ----------------------------------------------------------
    */

    //Unity connections
    public RawImage[] pickerImages;
    private enum PickerType {
        Main, H, S, V, R, G, B, A, Preview, PreviewAlpha
    }
    public InputField hexInput;
    public Dropdown modeDropdown;

    //public state
    public Color color {
        get { return this.bufferedColor.color; }
        set {
            this.bufferedColor.Set(value);
            if(OnColorChanged != null)
                OnColorChanged.Invoke(value);
        }
    }
    public MainPickingMode mode;
    public enum MainPickingMode {
        HS, HV, SH, SV, VH, VS
    }

    //private state
    private BufferedColor bufferedColor;
    private RawImage focusedPicker;
    private PickerType focusedPickerType;
    private MainPickingMode lastUpdatedMode;
    private bool typeUpdate;
    private bool pickerTexturesStandardized;

    //settings
    public Color startingColor = Color.white;
    public bool lightweightMode = false;
    private const float HUE_LOOP = 5.9999f;



    public delegate void ColorChanged(Color col);
    public event ColorChanged OnColorChanged;


    /*----------------------------------------------------------
    * ------------------------- UPKEEP -------------------------
    * ----------------------------------------------------------
    */

    private void Start() {
        this.bufferedColor = new BufferedColor(startingColor);
    }

    private void OnEnable() {
        if(this.bufferedColor == null)
            this.bufferedColor = new BufferedColor(startingColor);

        MakeTextures(true);
        MakeModeOptions();

        pickerTexturesStandardized = lightweightMode;
        UpdateMarkers();
    }

    private void OnDisable() {
        ClearTextures();
    }

    private void Update() {
        typeUpdate = false;

        if(lightweightMode && !pickerTexturesStandardized) {
            UpdateTextures(true);
            pickerTexturesStandardized = true;
        }
        else if(!lightweightMode && pickerTexturesStandardized) {
            UpdateTextures(false);
            pickerTexturesStandardized = false;
        }

        if(lastUpdatedMode != mode)
            ChangeMode(mode);

        MakeTextures(false);
    }

    /// <summary>
    /// Change picker that is being focused (and edited) using the pointer.
    /// </summary>
    /// <param name="i">Index of the picker image.</param>
    public void SetPointerFocus(int i) {
        if(i < 0 || i >= pickerImages.Length)
            Debug.LogWarning("No picker image available of type " + (PickerType)i + 
                ". Did you assign all the picker images in the editor?");
        else
            focusedPicker = pickerImages[i];
        focusedPickerType = (PickerType)i;
    }

    /// <summary>
    /// Update color based on the pointer position in the currently focused picker.
    /// </summary>
    /// <param name="e">Pointer event</param>
    public void PointerUpdate(BaseEventData e) {
        Vector2 v = GetNormalizedPointerPosition(focusedPicker.rectTransform, e);
        this.bufferedColor = PickColor(this.bufferedColor, focusedPickerType, v);

        if (OnColorChanged != null)
            OnColorChanged.Invoke(bufferedColor.color);

        UpdateMarkers();
        UpdateTextures();

        typeUpdate = true;
        UpdateHex();
    }

    /// <summary>
    /// Softly sanitize hex color input and apply it
    /// </summary>
    public void TypeHex(string input) {
        TypeHex(input, false);

        UpdateTextures();
        UpdateMarkers();
    }

    /// <summary>
    /// Strongly sanitize hex color input and apply it.
    /// (appends zeroes to fit proper length in the text box).
    /// </summary>
    public void FinishTypeHex(string input) {
        TypeHex(input, true);

        UpdateTextures();
        UpdateMarkers();
    }

    /// <summary>
    /// Change mode of the main, 2D picking image
    /// </summary>
    public void ChangeMode(int newMode) {
        ChangeMode((MainPickingMode)newMode);
    }

    /// <summary>
    /// Change mode of the main, 2D picking image
    /// </summary>
    public void ChangeMode(MainPickingMode mode) {
        this.mode = mode;

        UpdateTextures(lightweightMode);
        UpdateMarkers();

        UpdateMode(mode);
    }






    /*----------------------------------------------------------
    * --------------------- COLOR PICKING ----------------------
    * ----------------------------------------------------------
    * 
    * Get a new color that is the currently selected color but with 
    * one or two values changed. This is the core functionality of 
    * the picking images and the entire color picker script.
    */


    /// <summary>
    /// Get a color that is the current color, but changed by the given picker and value.
    /// </summary>
    /// <param name="type">Picker type to base change on</param>
    /// <param name="v">normalized x and y values (both values may not be used)</param>
    private BufferedColor PickColor(BufferedColor color, PickerType type, Vector2 v) {
        switch(type) {
        case PickerType.Main: return PickColorMain(color, v);

        case PickerType.Preview:
        case PickerType.PreviewAlpha:
        return color;

        default: return PickColor1D(color, type, v);
        }
    }

    private BufferedColor PickColorMain(BufferedColor color, Vector2 v) {
        return PickColorMain(color, this.mode, v);
    }

    private BufferedColor PickColor1D(BufferedColor color, PickerType type, Vector2 v) {
        bool horizontal = IsHorizontal(pickerImages[(int)type]);
        float value = horizontal ? v.x : v.y;
        return PickColor1D(color, type, value);
    }

    private BufferedColor PickColorMain(BufferedColor color, MainPickingMode mode, Vector2 v) {
        switch(mode) {
        case MainPickingMode.HS: return PickColor2D(color, PickerType.H, v.x, PickerType.S, v.y);
        case MainPickingMode.HV: return PickColor2D(color, PickerType.H, v.x, PickerType.V, v.y);
        case MainPickingMode.SH: return PickColor2D(color, PickerType.S, v.x, PickerType.H, v.y);
        case MainPickingMode.SV: return PickColor2D(color, PickerType.S, v.x, PickerType.V, v.y);
        case MainPickingMode.VH: return PickColor2D(color, PickerType.V, v.x, PickerType.H, v.y);
        case MainPickingMode.VS: return PickColor2D(color, PickerType.V, v.x, PickerType.S, v.y);
        default: return this.bufferedColor;
        }
    }

    private BufferedColor PickColor2D(BufferedColor color, PickerType type1, float value1, PickerType type2, float value2) {
        color = PickColor1D(color, type1, value1);
        color = PickColor1D(color, type2, value2);
        return color;
    }

    private BufferedColor PickColor1D(BufferedColor color, PickerType type, float value) {
        switch(type) {
        case PickerType.R: return color.PickR(value);
        case PickerType.G: return color.PickG(value);
        case PickerType.B: return color.PickB(value);
        case PickerType.H: return color.PickH(value * HUE_LOOP); 
        case PickerType.S: return color.PickS(value);
        case PickerType.V: return color.PickV(value);
        case PickerType.A: return color.PickA(value);
        default:
            throw new Exception("Picker type " + type + " is not associated with a single color value.");
        }
    }

    private BufferedColor GetDefaultColor(PickerType type) {
        switch(type) {
        case PickerType.Main: return GetDefaultColor(this.mode);
        case PickerType.H: return new BufferedColor(Color.red);
        case PickerType.S: return new BufferedColor(Color.red);
        case PickerType.A: return new BufferedColor(Color.white);
        default: return new BufferedColor();
        }
    }

    private BufferedColor GetDefaultColor(MainPickingMode mode) {
        return new BufferedColor(Color.red);
    }










    /*----------------------------------------------------------
    * -------------------- MARKER UPDATING ---------------------
    * ----------------------------------------------------------
    * 
    * Update positions of markers on each picking texture, 
    * indicating the currently selected values.
    */


    private void UpdateMarkers() {
        for(int i = 0; i < pickerImages.Length; i++) {
            RawImage image = GetImage(i);
            if(!image || !image.isActiveAndEnabled)
                continue;

            PickerType type = (PickerType)i;
            Vector2 v = GetValue(type);
            UpdateMarker(image, type, v);
        }
    }

    private void UpdateMarker(RawImage picker, PickerType type, Vector2 v) {
        switch(type) {
        case PickerType.Main:
        SetMarker(picker, v, true, true);
        break;

        case PickerType.Preview:
        case PickerType.PreviewAlpha:
        break;

        default:
        bool horizontal = IsHorizontal(picker);
        float value = horizontal ? v.x : v.y;
        SetMarker(picker, v, horizontal, !horizontal);
        break;
        }
    }

    private void SetMarker(RawImage picker, Vector2 v, bool setX, bool setY) {
        RectTransform marker = null;
        RectTransform offMarker = null;
        if(setX && setY)
            marker = GetMarker(picker, null);
        else if(setX) {
            marker = GetMarker(picker, "hor");
            offMarker = GetMarker(picker, "ver");
        }
        else if(setY) {
            marker = GetMarker(picker, "ver");
            offMarker = GetMarker(picker, "hor");
        }
        if(offMarker != null)
            offMarker.gameObject.SetActive(false);

        if(marker == null)
            return;

        marker.gameObject.SetActive(true);
        RectTransform parent = picker.rectTransform;
        Vector2 parentSize = parent.rect.size;
        Vector2 localPos = marker.localPosition;

        if(setX)
            localPos.x = (v.x - parent.pivot.x) * parentSize.x;
        if(setY)
            localPos.y = (v.y - parent.pivot.y) * parentSize.y;
        marker.localPosition = localPos;
    }

    private RectTransform GetMarker(RawImage picker, string search) {
        for(int i = 0; i < picker.transform.childCount; i++) {
            RectTransform candidate = picker.transform.GetChild(i).GetComponent<RectTransform>();
            string candidateName = candidate.name.ToLower();
            bool match = candidateName.Contains("marker");
            match &= string.IsNullOrEmpty(search)
                || candidateName.Contains(search);
            if(match)
                return candidate;
        }
        return null;
    }











    /*----------------------------------------------------------
    * -------------------- VALUE RETRIEVAL ---------------------
    * ----------------------------------------------------------
    * 
    * Get individual values associated with a picker image from the 
    * currently selected color.
    * This is needed to properly update markers.
    */
    
    private Vector2 GetValue(PickerType type) {
        switch(type) {

        case PickerType.Main: return GetValue(mode);

        case PickerType.Preview:
        case PickerType.PreviewAlpha:
        return Vector2.zero;

        default:
        float value = GetValue1D(type);
        return new Vector2(value, value);

        }
    }

    private float GetValue1D(PickerType type) {
        switch(type) {
        case PickerType.R: return this.bufferedColor.r;
        case PickerType.G: return this.bufferedColor.g;
        case PickerType.B: return this.bufferedColor.b;
        case PickerType.H: return this.bufferedColor.h / HUE_LOOP;
        case PickerType.S: return this.bufferedColor.s;
        case PickerType.V: return this.bufferedColor.v;
        case PickerType.A: return this.bufferedColor.a;
        default:
            throw new Exception("Picker type " + type + " is not associated with a single color value.");
        }
    }

    private Vector2 GetValue(MainPickingMode mode) {
        switch(mode) {
        case MainPickingMode.HS: return new Vector2(GetValue1D(PickerType.H), GetValue1D(PickerType.S));
        case MainPickingMode.HV: return new Vector2(GetValue1D(PickerType.H), GetValue1D(PickerType.V));
        case MainPickingMode.SH: return new Vector2(GetValue1D(PickerType.S), GetValue1D(PickerType.H));
        case MainPickingMode.SV: return new Vector2(GetValue1D(PickerType.S), GetValue1D(PickerType.V));
        case MainPickingMode.VH: return new Vector2(GetValue1D(PickerType.V), GetValue1D(PickerType.H));
        case MainPickingMode.VS: return new Vector2(GetValue1D(PickerType.V), GetValue1D(PickerType.S));
        default: throw new Exception("Unkown main picking mode: " + mode);
        }
    }











    /*----------------------------------------------------------
    * -------------------- TEXTURE UPDATING --------------------
    * ----------------------------------------------------------
    * 
    * Update picker image textures that show gradients of colors 
    * that the user can pick.
    */


    private void MakeTextures(bool forceNew = false) {
        if(pickerImages == null)
            pickerImages = new RawImage[0];

        foreach(PickerType type in Enum.GetValues(typeof(PickerType))) {
            RawImage image = GetImage((int)type);
            if(!image || !image.gameObject.activeInHierarchy)
                continue;

            Texture currentTexture = image.texture;
            Vector2 size = image.rectTransform.rect.size;
            int width = (int)size.x;
            int height = (int)size.y;

            bool needsNew = forceNew
                || currentTexture == null
                || width != currentTexture.width
                || height != currentTexture.height;

            if(needsNew) {
                image.texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                image.texture.name = image.name;
                image.texture.wrapMode = TextureWrapMode.Clamp;
                UpdateTexture(type, lightweightMode);
            }
        }
    }

    private void ClearTextures() {
        for(int i = 0; i < pickerImages.Length; i++) {
            pickerImages[i].texture = null;
        }
    }

    private void UpdateTextures(bool standardized = false) {
        foreach(PickerType type in Enum.GetValues(typeof(PickerType))) {
            if(IsPreviewType(type))
                UpdateTexture(type, false);
            else if(standardized || !lightweightMode)
                UpdateTexture(type, standardized);
        }
            
    }

    private void UpdateTexture(PickerType type, bool standardized) {
        RawImage image = GetImage((int)type);
        if(!image || !image.gameObject.activeInHierarchy)
            return;

        Texture2D tex = (Texture2D)(image.texture);
        int width = tex.width;
        int height = tex.height;
        Color[] pixels = new Color[width * height];

        for(int x = 0; x < width; x++) {
            float normX = (float)x / (width - 1);

            for(int y = 0; y < height; y++) {
                float normY = (float)y / (height - 1);
                Vector2 v = new Vector2(normX, normY);

                BufferedColor bc = this.bufferedColor;
                if(standardized)
                    bc = GetDefaultColor(type);
                bc = PickColor(bc, type, v);
                Color c = bc.color;

                if(!IsAlphaType(type))
                    c.a = 1f;

                int pixelIndex = x + y * width;
                pixels[pixelIndex] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
    }

    private RawImage GetImage(int index) {
        if(index < 0 || index >= pickerImages.Length)
            return null;
        RawImage toReturn = pickerImages[index];
        if(!toReturn.gameObject.activeInHierarchy)
            return null;
        return toReturn;
    }












    /*----------------------------------------------------------
    * ------------------ HEX INPUT UPDATING --------------------
    * ----------------------------------------------------------
    * 
    * Provides an input field for hexadecimal color values.
    * The user can type new values, or use this field to copy 
    * values picked via the picker images.
    */

    private void UpdateHex() {
        if(hexInput == null || !hexInput.gameObject.activeInHierarchy)
            return;
        hexInput.text = "#" + ColorUtility.ToHtmlStringRGB(this.color);
    }

    private void TypeHex(string input, bool appendZero) {
        if(typeUpdate)
            return;
        else
            typeUpdate = true;

        string newText = GetSanitizedHex(input, appendZero);
        hexInput.text = newText;
        if(hexInput.caretPosition == 0)
            hexInput.caretPosition = 1;
        string toParse = GetSanitizedHex(input, true);
        Color newColor;
        ColorUtility.TryParseHtmlString(toParse, out newColor);
        this.color = newColor;
    }












    /*----------------------------------------------------------
    * ---------------------- MODE UPDATING ---------------------
    * ----------------------------------------------------------
    * 
    * Allows user to change the 'Main picking mode' which determines 
    * the values shown on the main, 2D picking image.
    */

    private void MakeModeOptions() {
        if(modeDropdown == null || !modeDropdown.gameObject.activeInHierarchy)
            return;

        modeDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach(MainPickingMode mode in Enum.GetValues(typeof(MainPickingMode)))
            options.Add(mode.ToString());
        modeDropdown.AddOptions(options);

        UpdateMode(this.mode);
    }

    private void UpdateMode(MainPickingMode mode) {
        lastUpdatedMode = mode;
        if(modeDropdown == null || !modeDropdown.gameObject.activeInHierarchy)
            return;
        modeDropdown.value = (int)mode;
    }






    /*----------------------------------------------------------
    * ---------------- STATIC HELPER FUNCTIONS -----------------
    * ----------------------------------------------------------
    */

    private static bool IsPreviewType(PickerType type) {
        switch(type) {
        case PickerType.Preview: return true;
        case PickerType.PreviewAlpha: return true;
        default: return false;
        }
    }

    private static bool IsAlphaType(PickerType type) {
        switch(type) {
        case PickerType.A: return true;
        case PickerType.PreviewAlpha: return true;
        default: return false;
        }
    }

    /// <summary>
    /// Should given picker image be controlled horizontally?
    /// Returns true if the image is bigger in the horizontal direction.
    /// </summary>
    private static bool IsHorizontal(RawImage image) {
        Vector2 size = image.rectTransform.rect.size;
        return size.x >= size.y;
    }

    /// <summary>
    /// Santiive a given string so that it encodes a valid hex color string
    /// int he form #XXXXXX
    /// </summary>
    /// <param name="input">Input string</param>
    /// <param name="appendZeroes">Set 0 digits if the input string is too short</param>
    public static string GetSanitizedHex(string input, bool appendZeroes) {
        if(string.IsNullOrEmpty(input)) {
            if(appendZeroes)
                return "#000000";
            else
                return "#";
        }

        char[] toReturn = new char[7];
        toReturn[0] = '#';
        int j = 1;

        char[] chars = input.ToCharArray();
        for(int i = 0; i < chars.Length; i++) {
            char nextChar = char.ToUpper(chars[i]);
            bool validChar = char.IsNumber(nextChar);
            validChar |= nextChar >= 'A' && nextChar <= 'F';
            if(validChar)
                toReturn[j++] = nextChar;
        }
        if(appendZeroes) {
            while(j < 7)
                toReturn[j++] = '0';
        }
        return new string(toReturn);
    }

    /// <summary>
    /// Get normalized position of the given pointer event relative to the given rect.
    /// (e.g. return [0,1] for top left corner)
    /// </summary>
    private static Vector2 GetNormalizedPointerPosition(RectTransform rect, BaseEventData e) {
        Vector2 screenPoint = ((PointerEventData)e).position;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, screenPoint, null, out localPos);
        float x = Mathf.Clamp01((localPos.x / rect.rect.size.x) + rect.pivot.x);
        float y = Mathf.Clamp01((localPos.y / rect.rect.size.y) + rect.pivot.y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Get color from hue, saturation, value format
    /// </summary>
    /// <param name="hsv">Vector containing h, s and v values.</param>
    public static Color HSVToRGB(Vector3 hsv) {
        return HSVToRGB(hsv.x, hsv.y, hsv.z);
    }

    /// <summary>
    /// Get color from hue, saturation, value format
    /// </summary>
    /// <param name="h">hue value, ranging from 0 to 6; red to red</param>
    /// <param name="s">saturation, 0 to 1; gray to colored</param>
    /// <param name="v">value, 0 to 1; black to white</param>
    public static Color HSVToRGB(float h, float s, float v) {
        float c = s * v;
        float m = v - c;
        float x = c * (1f - Mathf.Abs(h % 2f - 1f)) + m;
        c += m;
        
        int range = Mathf.FloorToInt(h % 6f);
        
        switch(range) {
        case 0: return new Color(c, x, m);
        case 1: return new Color(x, c, m);
        case 2: return new Color(m, c, x);
        case 3: return new Color(m, x, c);
        case 4: return new Color(x, m, c);
        case 5: return new Color(c, m, x);
        default: return Color.black;
        }
    }

    /// <summary>
    /// Get hue, saturation and value of a color.
    /// Complementary to HSVToRGB
    /// </summary>
    public static Vector3 RGBToHSV(Color color) {
        float r = color.r;
        float g = color.g;
        float b = color.b;
        return RGBToHSV(r, g, b);
    }

    /// <summary>
    /// Get hue, saturation and value of a color.
    /// Complementary to HSVToRGB
    /// </summary>
    public static Vector3 RGBToHSV(float r, float g, float b) {
        float cMax = Mathf.Max(r,g,b);
        float cMin = Mathf.Min(r,g,b);
        float delta = cMax - cMin;
        float h = 0f;
        if(delta > 0f) {
            if(r >= b && r >= g)
                h = Mathf.Repeat((g - b) / delta, 6f);
            else if(g >= r && g >= b)
                h = (b - r) / delta + 2f;
            else if(b >= r && b >= g)
                h = (r - g) / delta + 4f;
        }
        float s = cMax == 0f ? 0f : delta / cMax;
        float v = cMax;
        return new Vector3(h, s, v);
    }








    /*----------------------------------------------------------
    * --------------------- HELPER CLASSES ---------------------
    * ----------------------------------------------------------
    */


    /// <summary>
    /// Encodes a color while buffering hue and saturation values.
    /// This is necessary since these values are singular for some 
    /// colors like unsaturated grays and would lead to undesirable 
    /// behaviour when moving sliders towards such colors.
    /// </summary>
    [Serializable]
    private class BufferedColor {
        public Color color;
        private float bufferedHue;
        private float bufferedSaturation;

        public float r { get { return color.r; } }
        public float g { get { return color.g; } }
        public float b { get { return color.b; } }
        public float a { get { return color.a; } }
        public float h { get { return bufferedHue; } }
        public float s { get { return bufferedSaturation; } }
        public float v { get { return RGBToHSV(color).z; } }


        public BufferedColor() {
            this.bufferedHue = 0f;
            this.bufferedSaturation = 0f;
            this.color = Color.black;
        }

        public BufferedColor(Color color) : this() {
            this.Set(color);
        }

        public BufferedColor(Color color, float hue, float sat) : this(color) {
            this.bufferedHue = hue;
            this.bufferedSaturation = sat;
        }

        public BufferedColor(Color color, BufferedColor source) : 
            this(color, source.bufferedHue, source.bufferedSaturation) {
            this.Set(color);
        }

        public void Set(Color color) {
            this.Set(color, this.bufferedHue, this.bufferedSaturation);
        }

        public void Set(Color color, float bufferedHue, float bufferedSaturation) {
            this.color = color;
            Vector3 hsv = RGBToHSV(color);

            bool hueSingularity = hsv.y == 0f || hsv.z == 0f;
            if(hueSingularity)
                this.bufferedHue = bufferedHue;
            else
                this.bufferedHue = hsv.x;

            bool saturationSingularity = hsv.z == 0f;
            if(saturationSingularity)
                this.bufferedSaturation = bufferedSaturation;
            else
                this.bufferedSaturation = hsv.y;
        }

        public BufferedColor PickR(float value) {
            Color toReturn = this.color;
            toReturn.r = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickG(float value) {
            Color toReturn = this.color;
            toReturn.g = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickB(float value) {
            Color toReturn = this.color;
            toReturn.b = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickA(float value) {
            Color toReturn = this.color;
            toReturn.a = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickH(float value) {
            Vector3 hsv = RGBToHSV(this.color);
            Color toReturn = HSVToRGB(value, hsv.y, hsv.z);
            toReturn.a = this.color.a;
            return new BufferedColor(toReturn, value, bufferedSaturation);
        }

        public BufferedColor PickS(float value) {
            Vector3 hsv = RGBToHSV(this.color);
            Color toReturn = HSVToRGB(bufferedHue, value, hsv.z);
            toReturn.a = this.color.a;
            return new BufferedColor(toReturn, bufferedHue, value);
        }

        public BufferedColor PickV(float value) {
            Vector3 hsv = RGBToHSV(this.color);
            Color toReturn = HSVToRGB(bufferedHue, bufferedSaturation, value);
            toReturn.a = this.color.a;
            return new BufferedColor(toReturn, bufferedHue, bufferedSaturation);
        }
    }
}
