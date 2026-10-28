using UnityEngine;

// Base de tudo que o jogador pode usar com [E].
// O objeto precisa de um collider trigger na camada "Interagivel".
public abstract class Interagivel : MonoBehaviour
{
    public float alturaAviso = 1.1f;

    public virtual string TextoAcao => "Examinar";
    public virtual bool Disponivel => true;
    public Vector3 PontoAviso => transform.position + Vector3.up * alturaAviso;

    public abstract void Interagir(Jogador jogador);
}
