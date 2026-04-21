using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    public Button play;
    public Button enlarge;
    public Button teleport; 
    public Button dash; 
    public Button obstruction; 
    public TextField code;

    public Label statusText;
    
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        //Sets references to the buttons and fields on the UI to give them functionality here
        play = root.Q<Button>("Play");
        enlarge = root.Q<Button>("Enlarge");
        teleport = root.Q<Button>("Teleport");
        obstruction = root.Q<Button>("Obstruction");
        dash = root.Q<Button>("Dash");
        code = root.Q<TextField>("LobbyCode");
        statusText = root.Q<Label>("LobbyStatus");
    }
}
