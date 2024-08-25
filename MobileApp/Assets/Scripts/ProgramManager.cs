using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using WebSocketSharp;

/// <summary>
/// �������� ���������
/// </summary>
public class MainConstants
{
    public static string LoadMessage = "...";
    public static string API = "ws://localhost:8080";
}

/// <summary>
/// �������� ������
/// </summary>
public class ProgramManager : MonoBehaviour
{
    [SerializeField] 
    private GameObject planeMarker;

    [SerializeField]
    private GameObject agent;

    [SerializeField]
    private TMP_InputField inputMsg;

    [SerializeField]
    private TMP_Text agentMessagePrototype;
    private TMP_Text agentMessage = null;

    private ARRaycastManager _raycastManager;
    private Button _btnAddAgent;
    private Button _btnAddMessage;

    private GameObject _mainCamera;

    private bool _isAdded = false;
    private bool _isLoading = false;

    // WebSocket �����������
    private WebSocket _ws;

    // ������ AR �����
    private List<ARRaycastHit> _hits;

    // ������� ������ ��� ���������
    private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();


    void Start()
    {
        _raycastManager = FindObjectOfType<ARRaycastManager>();
        planeMarker.SetActive(false);

        _btnAddAgent = GameObject.FindGameObjectWithTag("AddAgent").GetComponent<Button>();
        _btnAddAgent.onClick.AddListener(() => AddAgentHandler());

        // ��������� ������� �� ������ �������� ��������� ������
        _btnAddMessage = GameObject.FindGameObjectWithTag("AddMessage").GetComponent<Button>();
        _btnAddMessage.onClick.AddListener(() => AddMessageHandler());

        ConnectWebSocket();
    }

    void ConnectWebSocket()
    {
        if (_ws != null)
        { 
            _ws.Close();
        }

        // �������� ����������� WebSocket
        _ws = new WebSocket(MainConstants.API);
        
        if(_ws != null)
        {
            // ���������� ����������� ������ �� �������
            _ws.OnMessage += OnMessageHandler;

            // ����������� � �������
            _ws.ConnectAsync();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        SearchRays();

        // �������� �� ���� ������� ������ � ��������� �� ������ ����
        while (_actions.Count > 0)
        {
            if (_actions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        // ������� �� ���� �������
        _btnAddAgent.onClick.RemoveAllListeners();

        // ���������� �� �������
        if (_ws != null)
        {
            _ws.Close();
        }
    }

    void OnMessageHandler(object sender, MessageEventArgs e)
    {
        // ���������� ������� � �������
        _actions.Enqueue(() => receiveAgentMessage(sender, e));
    }

    /// <summary>
    /// ����� ����� � ��������� �������
    /// </summary>
    void SearchRays()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        _raycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), hits, TrackableType.Planes);

        if (hits.Count > 0)
        {
            planeMarker.transform.position = hits[0].pose.position;
            planeMarker.SetActive(true);
        }

        _hits = hits;
    }

    void AgentDestroy()
    {
        GameObject agent = GameObject.FindGameObjectWithTag("Agent");

        if (agent != null)
        {
            Destroy(agent);
        }

        GameObject agentText = GameObject.FindGameObjectWithTag("AgentMessage");

        if (agentText != null)
        {
            Destroy(agentText);
        }
    }

    void DebugCamera()
    {
        Vector3 position = Camera.main.transform.position;
        string message = $"Camera: x = {position.x}, y = {position.y}, z = {position.z}";
        Debug.Log(message);
    }

    /// <summary>
    /// ��������� ���������� ������ �� �����
    /// </summary>
    void AddAgentHandler()
    {
        // ���������� ���������� ��������� ��� ��������� ��������� ������
        if(agentMessage != null)
        {
            agentMessagePrototype.text = agentMessage.text;
        }

        if (_hits.Count > 0)
        {
            try
            {
                // �������� ����������� ������
                AgentDestroy();

                // ���������� �������� ������ ������
                Vector3 rotate = agent.transform.eulerAngles;
                rotate.y = 90f;
                agent.transform.rotation = Quaternion.Euler(rotate);

                // ���������� ������� ������ ������
                Vector3 agentPosition = _hits[0].pose.position;
                agentPosition.y = agentPosition.y + 1.05f;

                Quaternion rotation = agent.transform.rotation;
                rotation.y = rotation.y + 1.5f;

                // ��������������� � ������� ������������ ������ � �����, ������� � ���� ��� �������
                Instantiate(agent, _hits[0].pose.position, agent.transform.rotation);
                agentMessage = Instantiate(agentMessagePrototype, agentPosition, rotation);

                // �������� �������������
                if (!_isAdded)
                {
                    TextMeshProUGUI btnText = _btnAddAgent.GetComponentInChildren<TextMeshProUGUI>();
                    btnText.text = "�������� �������������� �������";
                    _isAdded = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }

    void AddMessageHandler()
    {
        if(agentMessage == null)
        {
            return;
        }

        if (!_isLoading)
        {
            sendAgentMessage();
        }
    }

    static void printComponents(GameObject root)
    {
        Component[] components = root.gameObject.GetComponents(typeof(Component));

        foreach (Component component in components)
        {
            Debug.Log(component.ToString());
        }
    }

    void sendAgentMessage()
    {
        agentMessage.text = MainConstants.LoadMessage;
        _isLoading = true;

        if (_ws != null && _ws.ReadyState == WebSocketState.Open)
        {
            _ws.Send(inputMsg.text.Trim());
        } else
        {
            agentMessage.text = "��������� ����������� � ���������";
            inputMsg.text = "";
        }
    }

    void receiveAgentMessage(object sender, MessageEventArgs e)
    {
        try
        {
            if (agentMessage != null)
            {
                if (e.Data.Trim().Length > 0)
                {
                    agentMessage.text = e.Data;
                }
                else
                {
                    agentMessage.text = "� ���� ��� ������ �� ���� ������";
                }
            }

            inputMsg.text = "";
            _isLoading = false;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }
}
