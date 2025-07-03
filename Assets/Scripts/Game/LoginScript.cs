using UnityEngine;
using TMPro;
using System.IO;

public class LoginScript : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    private string username;
    private string password;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ClickLogin()
    {
        username = usernameField.text;
        password = passwordField.text;
        string folderPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string filePath = Path.Combine(folderPath, "SavedInput.txt");

        File.WriteAllText(filePath, username + ", " + password);

        Debug.Log("Text saved to: " + filePath);
    }
}
