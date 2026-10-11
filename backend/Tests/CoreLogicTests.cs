using Shared;using System.Security.Claims;
namespace Tests;
public class CoreLogicTests
{
 [Fact] public void Overall_score_uses_configured_weights(){var sut=new OverallScoreCalculator();Assert.Equal(81.5m,sut.Calculate(new(90,70,80,80)));}
 [Theory][InlineData("Friendly staff and excellent service","Positive")][InlineData("Poor delay and lost baggage","Negative")][InlineData("Flight was ordinary","Neutral")]public void Sentiment_is_deterministic(string text,string expected)=>Assert.Equal(expected,new SentimentAnalyzer().Analyze(text).Sentiment);
 [Fact]public void Password_is_hashed_and_verifiable(){var hash=BCrypt.Net.BCrypt.HashPassword("secure-password");Assert.NotEqual("secure-password",hash);Assert.True(BCrypt.Net.BCrypt.Verify("secure-password",hash));}
 [Fact]public void Admin_role_claim_supports_authorization(){var user=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,"ADMIN")],"test"));Assert.True(user.IsInRole("ADMIN"));}
 [Fact]public void Statistics_calculation_stays_in_valid_range(){var input=new ScoreInput(100,0,100,0);var score=new OverallScoreCalculator().Calculate(input);Assert.InRange(score,0,100);Assert.Equal(60m,score);}
}
