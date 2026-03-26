using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        //Sets references to the buttons and fields on the UI to give them functionality here
        Button play = root.Q<Button>("Play");
        Button enlarge = root.Q<Button>("Enlarge");
        Button teleport = root.Q<Button>("Teleport");
        Button obstruction = root.Q<Button>("Obstruction");
        Button dash = root.Q<Button>("Dash");
        TextField Code = root.Q<TextField>("LobbyCode");

        var Ab = new AbilityAssignmentConfig();

        //functionality for all of the buttons and fields
        play.clicked += () => SceneManager.LoadScene("OverDriveZone");
        dash.clicked += () => Ab.SetAbility(0);
        obstruction.clicked += () => Ab.SetAbility(1);
        teleport.clicked += () => Ab.SetAbility(2);
        enlarge.clicked += () => Ab.SetAbility(3);
    }
}
