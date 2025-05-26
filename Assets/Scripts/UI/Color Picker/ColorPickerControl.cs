using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorPickerControl : MonoBehaviour
{
    [SerializeField]
    private SVImageControl svImageControl;

    public float currentHue;
    public float currentSat;
    public float currentVal;

    [SerializeField]
    private RawImage hueImage;
    [SerializeField]
    private RawImage satValImage;
    [SerializeField]
    private RawImage outputImage;

    [SerializeField]
    private Slider hueSlider;
    
    [SerializeField]
    private TMP_InputField hexInputField;


    private Texture2D hueTexture;
    private Texture2D svTexture;
    private Texture2D outputTexture;

    public Color pickedColor;
    private bool initialized = false;

    private void Start() 
    {
        if (initialized) return;
        CreateHueImage();
        CreateSVImage();
        CreateOutputImage();
        UpdateOutputImage();
    }

    private void CreateHueImage()
    {
        hueTexture = new Texture2D(1, 16);
        hueTexture.wrapMode = TextureWrapMode.Clamp;
        hueTexture.name = "HueTexture";

        for (int i = 0; i < hueTexture.height; i++)
        {
            hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / hueTexture .height, 1, 1f));
        }
        hueTexture.Apply();
        currentHue = 0;

        hueImage.texture = hueTexture;
    }

    private void CreateSVImage()
    {
        svTexture = new Texture2D(16, 16);
        svTexture.wrapMode = TextureWrapMode.Clamp;
        svTexture.name = "SatValTexture";
        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(
                                    currentHue,
                                    (float)x / svTexture.width,
                                    (float)y / svTexture.height));
            }
        }
        svTexture.Apply();
        currentSat = 0;
        currentVal = 0;
        satValImage.texture = svTexture;
    }

    private void CreateOutputImage()
    {
        outputTexture = new Texture2D(1, 16);
        outputTexture.wrapMode = TextureWrapMode.Clamp;
        outputTexture.name = "OutputTexture";

        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);
        for (int i = 0; i < outputTexture.height; i++)
        {
            outputTexture.SetPixel(0, i, currentColor);
        }
        outputTexture.Apply();
        outputImage.texture = outputTexture;
    }

    private void UpdateOutputImage()
    {
        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);
        for (int i = 0; i < outputTexture.height; i++)
        {
            outputTexture.SetPixel(0, i, currentColor);
        }
        
        outputTexture.Apply();

        hexInputField.text = ColorUtility.ToHtmlStringRGB(currentColor);
        // changeThis.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", currentColor);   
        pickedColor = currentColor;
    }

    public void SetSV(float S, float V)
    {
        currentSat = S;
        currentVal = V;
        UpdateOutputImage();
    }

    public void UpdateSVImage()
    {
        currentHue = hueSlider.value;
        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(
                                    currentHue,
                                    (float)x / svTexture.width,
                                    (float)y / svTexture.height));
            }
        }
        svTexture.Apply();
        UpdateOutputImage();
    }

    public void OnTextInput()
    {
        if (hexInputField.text.Length < 6) return;

        Color newCol;
        if (ColorUtility.TryParseHtmlString("#" + hexInputField.text, out newCol))
            Color.RGBToHSV(newCol, out currentHue, out currentSat, out currentVal);
        
        hueSlider.value = currentHue;
        hexInputField.text = "";

        svImageControl.SetPickerFromColor(newCol);
        UpdateOutputImage();
    }

    public void InitializeWithColor(Color color)
    {
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        hexInputField.text = ColorUtility.ToHtmlStringRGB(color);
        currentHue = h;
        if (!initialized)
        {
            CreateHueImage();
            CreateSVImage();
            CreateOutputImage();
        }
        currentSat = s;
        currentVal = v;
        hueSlider.value = h;
        svImageControl.SetPickerFromColor(color);

        initialized = true;
    }
}
