using System.Collections.Generic;
using UnityEngine;

public class PlayerManager
{
    // 서버에 접속한 플렝어들의 객체를 플레이어 ID 와 매칭하여 관리
    private readonly Dictionary<int, Player> _players = new();
    // 로컬 플레이어 변수
    private MyPlayer _myPlayer;

    // 싱글톤 패턴을 사용하여 전역적으로 접근 가능하도록 함
    public static PlayerManager Instance { get; } = new();

    // 플레이어 추가 함수
    public void Add(S_PlayerList packet)
    {
        // 리소스에서 플레이어 프리팹을 로드
        var obj = Resources.Load("Player");

        // 서버에서 받은 플레이어 리스트를 순회하며 플레이어 객체를 생성
        foreach (var p in packet.players)
        {
            // 게임 오브젝트를 생성
            var go = Object.Instantiate(obj) as GameObject;

            if (p.isSelf)
            {
                // 로컬 플레이어인 경우 MyPlayer 컴포넌트를 추가
                var myPlayer = go.AddComponent<MyPlayer>();
                // 플레이어 ID 를 설정
                myPlayer.PlayerId = p.playerId;
                // 플레이어의 위치를 설정
                myPlayer.transform.position = new Vector3(p.posX, p.posY, p.posZ);
                // 로컬 플레이어 변수에 할당
                _myPlayer = myPlayer;
            }
            else
            {
                // 로컬 플레이어가 아닌 경우 Player 컴포넌트를 추가
                var player = go.AddComponent<Player>();
                // 플레이어 ID 를 설정
                player.PlayerId = p.playerId;
                // 플레이어의 위치를 설정
                player.transform.position = new Vector3(p.posX, p.posY, p.posZ);
                // 플레이어 딕셔너리에 추가
                _players.Add(p.playerId, player);
            }
        }
    }

    // 플레이어 움직임 함수
    public void Move(S_BroadcastMove packet)
    {
        // 로컬 플레이어의 경우 최소 거리를 설정하여 움직임을 보정
        const float minDist = 1f;

        // 로컬 플레이어인 경우
        if (_myPlayer.PlayerId == packet.playerId)
        {
            // 로컬 플레이어의 위치가 서버에서 받은 위치와 일정 거리 이상 차이가 나는 경우 위치를 보정
            if (Vector3.Distance(_myPlayer.transform.position, new Vector3(packet.posX, packet.posY, packet.posZ)) >
                minDist)
                _myPlayer.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        }
        else
        {
            // 로컬 플레이어가 아닌 경우 플레이어 딕셔너리에서 플레이어를 찾아 위치를 보정
            if (_players.TryGetValue(packet.playerId, out var player))
                player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        }
    }

    // 플레이어 입장 함수
    public void EnterGame(S_BroadcastEnterGame packet)
    {
        // 로컬 플레이어인 경우 함수 종료
        if (packet.playerId == _myPlayer.PlayerId)
            return;

        // 리소스에서 플레이어 프리팹을 로드
        var obj = Resources.Load("Player");
        // 게임 오브젝트를 생성
        var go = Object.Instantiate(obj) as GameObject;

        // Player 컴포넌트를 추가
        var player = go.AddComponent<Player>();
        // 플레이어 위치 설정
        player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        // 플레이어 딕셔너리에 추가
        _players.Add(packet.playerId, player);
    }

    // 플레이어 퇴장 함수
    public void LeaveGame(S_BroadcastLeaveGame packet)
    {
        // 로컬 플레이어인 경우 함수 종료
        if (_myPlayer.PlayerId == packet.playerId)
        {
            // 로컬 플레이어 게임 오브젝트를 삭제
            Object.Destroy(_myPlayer.gameObject);
            // 로컬 플레이어 변수를 null 로 설정
            _myPlayer = null;
        }
        // 로컬 플레이어가 아닌 경우
        else
        {
            // 플레이어 딕셔너리에서 플레이어를 찾아 게임 오브젝트를 삭제
            if (!_players.TryGetValue(packet.playerId, out var player)) return;
            Object.Destroy(player.gameObject);
            _players.Remove(packet.playerId);
        }
    }
}