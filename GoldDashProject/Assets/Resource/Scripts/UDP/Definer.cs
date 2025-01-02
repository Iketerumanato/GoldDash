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
        IPC,
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
        PP,
    }

    /// <summary>
    /// RoughID, ActionPacketの種別を大まかに分類する
    /// </summary>
    public enum RID : byte
    { 
        /// <summary>
        /// Move, プレイヤーが移動した
        /// </summary>
        MOV,
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
        /// ゲーム開始まではパケットを送信する用事が無いため、定期的にハローパケットを飛ばして存在を確かめ合う
        /// </summary>
        HELLO,
        /// <summary>
        /// Prepare(d) for Start Game, （サーバーからクライアントへ）ゲーム開始の準備をしてください。または（クライアントからサーバーへ）ゲーム開始の準備ができました。
        /// </summary>
        PSG,
        /// <summary>
        /// 通信を切断します。
        /// </summary>
        DISCONNECT,
        /// <summary>
        /// Start Game, ゲームを開始します
        /// </summary>
        STG,
        /// <summary>
        /// End Game, ゲームを終了します
        /// </summary>
        EDG,
    }

    /// <summary>
    /// Request Detail ID, リクエスト内容の詳細な内容
    /// </summary>
    public enum REID : byte
    {
        /// <summary>
        /// 金貨の山を手に入れようとしました
        /// </summary>
        GET_GOLDPILE,
        /// <summary>
        /// 空振りました
        /// </summary>
        MISS,
        /// <summary>
        /// 正面からパンチしました
        /// </summary>
        HIT_FRONT,
        /// <summary>
        /// 背面からパンチしました
        /// </summary>
        HIT_BACK,
        /// <summary>
        /// 宝箱の開錠に成功しました
        /// </summary>
        OPEN_CHEST_SUCCEED,
        /// <summary>
        /// 魔法を使用しました。
        /// </summary>
        USE_MAGIC,
        /// <summary>
        /// サーバー内部専用。雷を生成してください。
        /// </summary>
        INTERNAL_THUNDER,
    }

    /// <summary>
    /// Execute Detail ID, 執行命令の詳細な内容
    /// </summary>
    public enum EDID : byte
    { 
        /// <summary>
        /// このActorをスポーンさせろ
        /// </summary>
        SPAWN_ACTOR,
        /// <summary>
        /// 宝箱をスポーンさせろ
        /// </summary>
        SPAWN_CHEST,
        /// <summary>
        /// 金貨の山をスポーンさせろ
        /// </summary>
        SPAWN_GOLDPILE,
        /// <summary>
        /// 雷をスポーンさせろ。
        /// </summary>
        SPAWN_THUNDER,
        /// <summary>
        /// このActorを削除しろ
        /// </summary>
        DELETE_ACTOR,
        /// <summary>
        /// このエンティティを削除しろ
        /// </summary>
        DESTROY_ENTITY,
        /// <summary>
        /// このActorの所持金を変更しろ
        /// </summary>
        EDIT_GOLD,
        /// <summary>
        /// このActorにパンチさせろ
        /// </summary>
        PUNCH,
        /// <summary>
        /// このActorにパンチを正面からヒットさせろ
        /// </summary>
        HIT_FRONT,
        /// <summary>
        /// このActorにパンチを背面からヒットさせろ
        /// </summary>
        HIT_BACK,
        /// <summary>
        /// このActorにこの魔法（の巻物）を与えろ
        /// </summary>
        GIVE_MAGIC,
        /// <summary>
        /// このActorの魔法をひとつ消費させろ
        /// </summary>
        CONSUME_MAGIC,
        /// <summary>
        /// このアクターをスタンさせろ
        /// </summary>
        GIVE_STATUS_STUN,
        /// <summary>
        /// このアクターをゴールドダッシュ状態にしろ
        /// </summary>
        GIVE_STATUS_GOLDDASH,
    }

    /// <summary>
    /// Magic ID, 魔法の種類を表すID
    /// </summary>
    public enum MID : int //ActionPacketのValueに載せるのでint型
    {
        NONE = -1,
        THUNDER,
        DASH,
        TELEPORT,
    }
}
