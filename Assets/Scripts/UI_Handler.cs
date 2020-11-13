using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NetworkMessages;

public class UI_Handler : MonoBehaviour
{
    GameObject console;
    GameObject toggleTxt;
    GameObject consoleMsg;
    List<NetworkPlayer> logs = new List<NetworkPlayer>();
    string msg;

    NetworkPlayer FindPlayerLog(NetworkPlayer player)
    {
        foreach(NetworkPlayer p in logs)
        {
            if(p.id == player.id)
            {
                return p;
            }
        }
        return null;
    }
        
    public void PlayerLog(NetworkPlayer value)
    {
        if(FindPlayerLog(value) == null)
        {
            logs.Add(value);
        }
        else
        {
            FindPlayerLog(value).pos = value.pos;
            FindPlayerLog(value).destroy = value.destroy;
        }
    }

    void Start()
    {
        console = transform.GetChild(0).gameObject;
        consoleMsg = console.transform.GetChild(0).gameObject;
        toggleTxt = transform.GetChild(1).gameObject.transform.GetChild(1).gameObject;
    }

    public void UI_ToggleConsole()
    {
        if(console.activeSelf)
        {
            console.SetActive(false);
            toggleTxt.GetComponent<Text>().text = "Show";
        }
        else
        {
            console.SetActive(true);
            toggleTxt.GetComponent<Text>().text = "Hide";
        }
    }
    
    void UpdateConsole()
    {
        if (logs.Count > 0)
        {
            for(int i = 0; i < logs.Count; i++)
            {
                if (!logs[i].destroy)
                {
                    msg += "Player: " + logs[i].id + " Position: " + logs[i].pos;
                }
                else
                {
                    msg += "Player: " + logs[i].id + "NOT CONNECTED";
                }
                msg += System.Environment.NewLine;
            }
        }
        consoleMsg.GetComponent<Text>().text = msg;
    }

    void Update()
    {
        UpdateConsole();
    }
}
