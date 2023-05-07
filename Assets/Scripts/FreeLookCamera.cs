using Cinemachine;
using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    // 시네머신 프리룩 카메라
    private CinemachineFreeLook _freeLookCamera;
    // 싱글톤
    public static FreeLookCamera Instance { get; private set; }

    private void Awake()
    {
        // 싱글톤
        Instance = this;
        // 카메라 컴포넌트
        _freeLookCamera = GetComponent<CinemachineFreeLook>();
    }

    public void SetTarget(Transform target)
    {
        // 카메라 타겟 설정
        _freeLookCamera.Follow = target;
        _freeLookCamera.LookAt = target;
    }
}