using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetGame : MonoBehaviour
{
    TCP tcp;

    public GameObject connect;
    public GameObject game;

    void Start()
    {
        tcp = GetComponent<TCP>();
        connect.SetActive(true);
        game.SetActive(false);
    }

    void Update()
    {
        if (tcp.IsConnect() && connect != null)
        {
            connect.SetActive(false);
            game.SetActive(true);
        }
    }
}