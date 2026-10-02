using UnityEngine;

/*
 * CubicBezierMath holds De Casteljau point and tangent sampling for four
 * control points. CubicBezierCurve supplies their current scene positions.
 */

public static class CubicBezierMath
{
    public static Vector3 SamplePoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // TODO Slice 3.1: evaluate the cubic at t with De Casteljau. Slice 3 is on your own.
        // Check: DeCasteljauCubic_SamplesPointFromEquivalentCubicFormula passes.
        // Next: Slice 3.2 in Demo/CubicBezierCurve.cs.
        Vector3 A = Vector3.Lerp(p0, p1, t);
        Vector3 B = Vector3.Lerp(p1, p2, t);
        Vector3 C = Vector3.Lerp(p2, p3, t);
        Vector3 D = Vector3.Lerp(A, B, t);
        Vector3 E = Vector3.Lerp(B, C, t);
        return Vector3.Lerp(D, E, t);
    }

    public static Vector3 SampleTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // TODO Slice 3.5: return the cubic derivative at t from your De Casteljau
        // construction. Do not normalize it.
        // Stuck? Show ChatGPT your construction and ask: "Differentiate this with respect
        // to t using the product rule. Explain each step." Then derive it yourself.
        // Check: DeCasteljauCubic_SamplesTangentFromFinalInterpolationSegment passes.
        // Next: Slice 3.6 in Demo/CubicBezierCurve.cs.
        Vector3 A = Vector3.Lerp(p0, p1, t);
        Vector3 B = Vector3.Lerp(p1, p2, t);
        Vector3 C = Vector3.Lerp(p2, p3, t);
        Vector3 D = Vector3.Lerp(A, B, t);
        Vector3 E = Vector3.Lerp(B, C, t);
        return 3 * (E - D);
    }
}