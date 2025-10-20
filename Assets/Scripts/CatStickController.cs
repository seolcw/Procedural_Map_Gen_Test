using UnityEngine;

public class CatStickController : MonoBehaviour
{
    public GameObject player;
    public float offset = 0.3f;

    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if(mainCam == null)
        {
            Debug.Log("Main Camera is not found"); // 예외사항 항상 습관화 하기
        }
    }

    void Update()
    {
        if (mainCam == null || player == null) return;

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3 dir = (mouseWorldPos - player.transform.position).normalized;
        this.transform.position = player.transform.position + dir * offset;
    }
}
