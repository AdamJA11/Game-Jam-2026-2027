using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   


public class Switch_Image : MonoBehaviour
{
    
    public Sprite[] sprites;      
    public Image targetImage; 
    public GameObject lastImageObject;  
      
    
    private int currentIndex = 0; 

    public void SwitchImage()
    {
        if (sprites.Length == 0) return; 

        currentIndex = (currentIndex + 1) % sprites.Length; 
        
        
        targetImage.sprite = sprites[currentIndex]; 
        if (currentIndex == sprites.Length - 1)
        {
            targetImage.gameObject.GetComponent<Image>().enabled = false;
            lastImageObject.SetActive(true);
        }
        
        
        targetImage.SetNativeSize();
    }


}
