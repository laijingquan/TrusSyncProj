using UnityEngine;
using System.Collections;

public class TutorialMenu : Photon.PunBehaviour {

	// Use this for initialization
	void Start () {
        PhotonNetwork.ConnectUsingSettings("v1.0");
        PhotonNetwork.automaticallySyncScene = true;
	}

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinOrCreateRoom("room1", null, null);
    }
    // Update is called once per frame

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 30), "players" + PhotonNetwork.playerList.Length);
        if(PhotonNetwork.isMasterClient&&GUI.Button(new Rect(10,40,100,30),"start"))
        {
            PhotonNetwork.LoadLevel("Tutorial/Game");
        }
    }
}
