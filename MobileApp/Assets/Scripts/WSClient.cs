using WebSocketSharp;
using UnityEngine;

public class WSClient : MonoBehaviour
{
    WebSocket ws;

    void Start()
    {
        Debug.Log("HELLO");

        ws = new WebSocket("ws://localhost:8080");
        ws.OnMessage += (sender, e) => {
            Debug.Log("Message received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
        };

        ws.Connect();
    }

    void Update()
    {
        if(ws == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ws.Send("Hello");
        }
    }

    private void OnDestroy()
    {
        if(ws != null)
        {
            ws.Close();
        }
    }
}
