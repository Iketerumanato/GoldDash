using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 定数宣言用クラス
/// </summary>
public static class Definer

{
    /// <summary>
    /// PacketType, パケットの種類
    /// </summary>
    public enum PT : byte
    { 
        /// <summary>
        /// InitPacketClient, クライアントからサーバーへ送る初期設定用パケット。
        /// </summary>
        IPC = 0,
        /// <summary>
        /// InitPAcketServer, サーバーからクライアントへ送る初期設定用パケット。
        /// </summary>
        IPS,
        /// <summary>
        /// ActionPacket, 通知やアクションの実行申請・執行命令などを行う。
        /// </summary>
        AP,
        /// <summary>
        /// PositionPacket, 定期的に各プレイヤーキャラクターの座標をサーバーから全クライアントに送信するためのパケット
        /// </summary>
        PP, //P
    }

    /// <summary>
    /// RoughID, ActionPacketの種別を大まかに分類する
    /// </summary>
    public enum RID : byte
    { 
        /// <summary>
        /// Move, プレイヤーが移動した
        /// </summary>
        MOV = 0,
        /// <summary>
        /// Notice, ゲーム開始・終了などを知らせる。
        /// </summary>
        NOT,
        /// <summary>
        /// Request, クライアントからサーバーにアクションをリクエストする
        /// </summary>
        REQ,
        /// <summary>
        /// Execute, サーバーで承認（実行結果を確認）したアクションを各クライアントへ反映させる。
        /// </summary>
        EXE,
    }

    /// <summary>
    /// Notice Detail ID, お知らせの詳細な分類
    /// </summary>
    public enum NDID : byte
    { 
        /// <summary>
        /// MOVなど、DetailIDを必要としない場合
        /// </summary>
        NONE = 0,
        /// <summary>
        /// ゲーム開始まではパケットを送信する用事が無いため、定期的にハローパケットを飛ばして存在を確かめ合う
        /// </summary>
        HELLO,
        /// <summary>
        /// ゲームを開始します
        /// </summary>
        STG,
        /// <summary>
        /// ゲームを終了します
        /// </summary>
        EDG,
    }

    /// <summary>
    /// Execute Detail ID, 執行命令の詳細な内容
    /// </summary>
    public enum EDID : byte
    { 
        /// <summary>
        /// Actorをスポーンさせろ
        /// </summary>
        SPAWN = 0,
    }
}
