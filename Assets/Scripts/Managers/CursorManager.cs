using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTexture;

    public bool centerHotspot = true;
    public Vector2 customHotSpot = Vector2.zero;

    void Start()
    {
        if (cursorTexture != null)
        {
            Vector2 hotSpot = centerHotspot
                ? new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f)
                : customHotSpot;

            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }
    }
}