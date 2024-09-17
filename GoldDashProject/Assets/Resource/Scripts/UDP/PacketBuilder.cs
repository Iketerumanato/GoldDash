using System;
using System.Collections;
using System.Collections.Generic;

public static class PacketBuilder
{
    //パケットの種類
    public enum PACKET_TYPE : byte
    { 
        INIT_PACKET_CLIENT = 0,
        INIT_PACKET_SERVER,
        ACTION_PACKET,
        POSITION_PACKET,
    }

    //アクションの種類
    public enum ACTION_ROUGH_ID : byte
    { 
        NOTICE = 0, //お知らせ
        REQUEST, //プレイヤーからのアクションリクエスト
        EXECUTE, //サーバーによって承認されたアクションの執行命令
        MOVE, //プレイヤーの移動
    }

    public enum NOTICE_DETAIL_ID : byte
    { 
        HELLO, //定期的にハローパケットを飛ばして存在を確かめ合う
        MATCHING_COMPLETED, //マッチングが終わった
        //今後増やす
    }


}
