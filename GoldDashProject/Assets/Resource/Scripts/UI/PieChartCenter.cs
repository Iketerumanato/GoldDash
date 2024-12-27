using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieChartCenter : MonoBehaviour
{
    [SerializeField] private RectTransform pieChartCenter; // 円グラフの中心
    [SerializeField] private float radius = 100f; // 円グラフの半径
    [SerializeField] private float[] fillAmounts; // 各セグメントのFillAmount（0.0～1.0）

    private void Start()
    {
        CalculateSegmentCenters();
    }

    private void CalculateSegmentCenters()
    {
        float startAngle = 0f; // 初期角度
        Vector3 chartCenter = pieChartCenter.position;

        for (int i = 0; i < fillAmounts.Length; i++)
        {
            // 各セグメントの角度を計算
            float segmentAngle = fillAmounts[i] * 360f;
            float endAngle = startAngle + segmentAngle;

            // セグメントの中心角を計算
            float centerAngle = (startAngle + endAngle) / 2f;

            // ラジアンに変換
            float radians = centerAngle * Mathf.Deg2Rad;

            // 中心点の座標を計算
            float x = chartCenter.x + radius * Mathf.Cos(radians);
            float y = chartCenter.y + radius * Mathf.Sin(radians);
            Vector3 segmentCenter = new Vector3(x, y, chartCenter.z);

            // 中心点をデバッグ出力
            Debug.Log($"Segment {i + 1} Center: {segmentCenter}");

            // 開始角度を更新
            startAngle = endAngle;
        }
    }
}
