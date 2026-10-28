using UnityEngine;

[CreateAssetMenu(menuName = "Mansao/Documento")]
public class DocumentoDados : ScriptableObject
{
    public string titulo;
    [TextArea(6, 20)] public string texto;
}
