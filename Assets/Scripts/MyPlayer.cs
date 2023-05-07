using System.Collections;
using UnityEngine;

public class MyPlayer : Player
{
    // 네트워크 매니저
    private NetworkManager _network;
    // 이동 속도
    private const float Speed = 5f;
    // 이전 위치
    private Vector3 _prevPos;

    private void Start()
    {
        // 움직임 패킷 전송 코루틴 시작
        StartCoroutine(nameof(CoSendPacket));
        // 네트워크 매니저 가져오기
        _network = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        // 카메라 타겟 설정
        FreeLookCamera.Instance.SetTarget(transform);
    }

    private void Update()
    {
        // 키보드 입력
        var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        
        // 카메라가 없으면 리턴
        if (Camera.main == null) return;
        
        // 카메라의 방향을 기준으로 이동 방향을 설정
        var moveVelocity = Camera.main.transform.TransformDirection(input.normalized * Speed);
        moveVelocity.y = 0;
        
        // 이동
        transform.position += moveVelocity * Time.deltaTime;
    }

    private IEnumerator CoSendPacket()
    {
        while (true)
        {
            // 1초에 20번씩 이동 패킷 전송
            yield return new WaitForSeconds(1f / 20f);
            
            // 이전 위치와 현재 위치가 같으면 패킷 전송하지 않음
            if (_prevPos == transform.position) continue;
            
            // 이동 패킷 전송
            var position = transform.position;
            _prevPos = position;
            
            // 이동 패킷 생성
            var movePacket = new C_Move(position);
            _network.Send(movePacket.Write());
        }
    }
}