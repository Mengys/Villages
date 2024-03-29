using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StartButton : MonoBehaviour
{
    public void HideButton() {
        gameObject.SetActive(false);
    }
}
