using UnityEngine;

/*
 * QuadraticBezierMath holds three ways to sample a quadratic Bezier:
 * De Casteljau, Bernstein basis, and power basis. Callers supply the points,
 * so this class does not depend on scene objects. Tangents are derivatives,
 * not unit directions.
 */

public static class QuadraticBezierMath
{
    public static Vector3 SamplePointDeCasteljau(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // TODO Slice 1.4: evaluate the quadratic at t with the De Casteljau construction.
        // Check: DeCasteljauQuadratic_SamplesPointFromEquivalentQuadraticFormula passes.
        // Next: Slice 1.5 in Demo/QuadraticBezierCurve.cs.
        Vector3 A = Vector3.Lerp(p0, p1, t);
        Vector3 B = Vector3.Lerp(p1, p2, t);
        return Vector3.Lerp(A, B, t);
    }

    public static Vector3 SampleTangentDeCasteljau(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // TODO Slice 2.2: return the derivative at t from your De Casteljau construction.
        // Do not normalize it.
        // Stuck? Show ChatGPT your construction and ask: "Differentiate this with respect
        // to t using the product rule. Explain each step." Then derive it yourself.
        // Check: DeCasteljauQuadratic_SamplesTangentFromFinalInterpolationSegment passes.
        // Next: Slice 2.3 in Demo/QuadraticBezierCurve.cs.
        Vector3 A = Vector3.Lerp(p0, p1, t);
        Vector3 B = Vector3.Lerp(p1, p2, t);
        return 2 * (B - A);
    }

    // B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2
    public static Vector3 SamplePointBernstein(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // TODO Slice 6.1: evaluate the Bernstein equation above.
        // Check: BernsteinQuadratic_SamplesPointFromEquivalentQuadraticFormula passes.
        // Next: Slice 6.2 in Demo/QuadraticBezierCurve.cs.
        float one_min_t = 1 - t;
        return (one_min_t* one_min_t * p0) + (2 * one_min_t * t * p1) + (t * t * p2);
    }

    public static Vector3 SampleTangentBernstein(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // TODO Slice 6.3: return the derivative of the Bernstein equation above.
        // Do not normalize it.
        // Stuck? Show ChatGPT the equation and ask: "Differentiate this with respect
        // to t using the product rule. Explain each step." Then derive it yourself.
        // Check: BernsteinQuadratic_SamplesTangentFromDerivativeFormula passes.
        // Next: Slice 6.4 in Demo/QuadraticBezierCurve.cs.
        return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
    }

    // C0 = P0
    // C1 = 2(P1 - P0)
    // C2 = P0 - 2P1 + P2
    //
    // Compute once when the control points change;
    // reuse the coefficients for every t.
    public static void ComputePowerBasisCoefficients(
        Vector3 p0, Vector3 p1, Vector3 p2,
        out Vector3 c0, out Vector3 c1, out Vector3 c2)
    {
        // TODO Slice 7.1: compute C0, C1, C2 from the equations above.
        // Check: points (0,0,0), (2,3,0), (4,0,0) give (0,0,0), (4,6,0), (0,-6,0).
        // Next: Slice 7.2 below. No test turns green from 7.1 alone.
        c0 = p0;
        c1 = 2 * (p1 - p0);
        c2 = p0 - 2 * p1 + p2;
    }

    // P(t) = C0 + C1 t + C2 t^2
    public static Vector3 SamplePointPowerBasis(Vector3 c0, Vector3 c1, Vector3 c2, float t)
    {
        // TODO Slice 7.2: evaluate P(t) from the coefficients.
        // Check: PowerBasisQuadratic_SamplesPointFromEquivalentQuadraticFormula passes.
        // Next: Slice 7.3 in Demo/QuadraticBezierCurve.cs, Start.
        return c0 + c1 * t + c2 * t * t;
    }

    // P'(t) = C1 + 2 C2 t
    public static Vector3 SampleTangentPowerBasis(Vector3 c1, Vector3 c2, float t)
    {
        // TODO Slice 7.4: evaluate P'(t) from the coefficients. Do not normalize it.
        // Check: PowerBasisQuadratic_SamplesTangentFromDerivativeFormula passes.
        // All 17 tests now pass. No scene hookup: the adapters keep Bernstein.
        // Next: open Main Game, Slice 8.1 in Game/ThrownAxe.cs. </> end of Slice 7
        return c1 + 2 * c2 * t;
    }
}
