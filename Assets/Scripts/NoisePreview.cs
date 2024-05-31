using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoisePreview : MonoBehaviour
{
    [SerializeField] private Renderer _textureRenderer;


    public void DrawTexture(Texture2D texture)
    {
        _textureRenderer.sharedMaterial.mainTexture = texture;
        _textureRenderer.transform.localScale = new Vector3(texture.width/10, 1, texture.height/10);
    }

}
