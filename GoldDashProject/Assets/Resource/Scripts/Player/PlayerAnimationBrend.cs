using UnityEngine;

public class PlayerAnimationBrend : MonoBehaviour
{
    [SerializeField, Range(0, 1)] float m_moveSpeed = 0f;
    Animator m_animator;

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        m_animator.SetFloat("Blend", m_moveSpeed);
    }
}