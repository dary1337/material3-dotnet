using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;
using Xunit;

namespace Material3.WinForms.Tests {
    public class TokenTests {
        [Fact]
        public void StateLayerOpacities_MatchM3Spec() {
            Assert.Equal(0.08, StateLayers.Hover);
            Assert.Equal(0.12, StateLayers.Focus);
            Assert.Equal(0.12, StateLayers.Pressed);
            Assert.Equal(0.16, StateLayers.Dragged);
        }

        [Fact]
        public void ElevationTint_Level0IsIdentity_OpacityGrowsWithLevel() {
            Assert.Equal(0.0, Elevation.TintOpacity[0]);
            for (int level = 1; level <= 5; level++) {
                Assert.True(Elevation.TintOpacity[level] > Elevation.TintOpacity[level - 1]);
            }
        }

        [Fact]
        public void ShadowMargin_IsZeroAtLevel0_PositiveAbove() {
            Assert.Equal(0, Elevation.ShadowMargin(0));
            for (int level = 1; level <= 5; level++) {
                Assert.True(Elevation.ShadowMargin(level) > 0);
            }
        }

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(1.0, 1.0)]
        public void Easing_PinsEndpoints(double t, double expected) {
            Assert.Equal(expected, Motion.Standard.Evaluate(t), 3);
            Assert.Equal(expected, Motion.EmphasizedDecelerate.Evaluate(t), 3);
            Assert.Equal(expected, Motion.EmphasizedAccelerate.Evaluate(t), 3);
        }

        [Fact]
        public void EmphasizedDecelerate_FrontLoadsProgress() {
            Assert.True(Motion.EmphasizedDecelerate.Evaluate(0.3) > 0.6);
            Assert.True(Motion.EmphasizedAccelerate.Evaluate(0.3) < 0.2);
        }

        [Fact]
        public void Easing_IsMonotonic() {
            foreach (CubicBezier easing in new[] {
                Motion.Standard, Motion.StandardDecelerate, Motion.StandardAccelerate,
                Motion.EmphasizedDecelerate, Motion.EmphasizedAccelerate,
            }) {
                double previous = 0;
                for (double t = 0; t <= 1.0001; t += 0.05) {
                    double value = easing.Evaluate(t);
                    Assert.True(value >= previous - 1e-6, $"Easing not monotonic at t={t}");
                    previous = value;
                }
            }
        }

        [Fact]
        public void TypeScale_HasAllFifteenStyles() {
            TextStyle[] styles = {
                TypeScale.DisplayLargeStyle, TypeScale.DisplayMediumStyle, TypeScale.DisplaySmallStyle,
                TypeScale.HeadlineLargeStyle, TypeScale.HeadlineMediumStyle, TypeScale.HeadlineSmallStyle,
                TypeScale.TitleLargeStyle, TypeScale.TitleMediumStyle, TypeScale.TitleSmallStyle,
                TypeScale.BodyLargeStyle, TypeScale.BodyMediumStyle, TypeScale.BodySmallStyle,
                TypeScale.LabelLargeStyle, TypeScale.LabelMediumStyle, TypeScale.LabelSmallStyle,
            };
            foreach (TextStyle style in styles) {
                Assert.NotNull(style.Font);
                Assert.True(style.LineHeight > 0);
            }
            Assert.True(TypeScale.DisplayLarge.SizeInPoints > TypeScale.DisplayMedium.SizeInPoints);
            Assert.True(TypeScale.BodyLarge.SizeInPoints > TypeScale.BodyMedium.SizeInPoints);
            Assert.True(TypeScale.BodyMedium.SizeInPoints > TypeScale.BodySmall.SizeInPoints);
        }
    }
}
