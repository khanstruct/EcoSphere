using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // Visualizer overlays: recipe threshold guides behind the element bars, and the star score strip along
    // the top of the frame. Thresholds come from ElementStandings and the world's rules; looks from VisualizerStyle.
    public sealed partial class EcoSpherePrototype
    {
        RectTransform matchZone, selfZone, matchLine, selfLine, scoreTrack, scoreFill;
        TMP_Text matchLineLabel, selfLineLabel, scoreLabel;
        readonly List<Image> scoreStars = new List<Image>();
        readonly List<bool> scoreStarsLit = new List<bool>();
        float guideLeft, guideRight, shownScore;
        string pinnedMark;

        // Called right after the frame so the zones and lines sit behind the bars.
        void BuildThresholdGuides(RectTransform strip)
        {
            matchZone = Panel("Match zone", strip, new Vector2(.5f, 0), Vector2.zero, Vector2.zero, visualizerStyle.matchZoneColor);
            selfZone = Panel("Self-match zone", strip, new Vector2(.5f, 0), Vector2.zero, Vector2.zero, visualizerStyle.selfZoneColor);
            matchLine = Panel("Match line", strip, new Vector2(.5f, 0), Vector2.zero, Vector2.zero, visualizerStyle.matchLineColor);
            selfLine = Panel("Self-match line", strip, new Vector2(.5f, 0), Vector2.zero, Vector2.zero, visualizerStyle.selfLineColor);
            foreach (var rect in new[] { matchZone, selfZone, matchLine, selfLine }) rect.pivot = new Vector2(.5f, 0);
            matchLineLabel = GuideLabel("Match line label", strip, visualizerStyle.matchLineColor);
            selfLineLabel = GuideLabel("Self-match line label", strip, visualizerStyle.selfLineColor);
            pinnedMark = font && font.HasCharacter('▲') ? " ▲" : " ^";
        }

        TMP_Text GuideLabel(string name, RectTransform strip, Color color)
        {
            var label = Label(name, strip, new Vector2(.5f, 0), Vector2.zero, new Vector2(90, 16), "", 11);
            // Right-aligned at the right end, created before the bars so bar totals draw over it if they meet.
            label.alignment = TextAlignmentOptions.BottomRight;
            label.color = new Color(color.r, color.g, color.b, Mathf.Max(color.a, .7f));
            label.rectTransform.pivot = new Vector2(1, 0);
            return label;
        }

        // Called after the bars: a slim strip along the top of the frame.
        void BuildScoreStrip(RectTransform strip)
        {
            scoreStars.Clear(); scoreStarsLit.Clear();
            scoreTrack = Panel("Star score track", strip, new Vector2(.5f, 1), Vector2.zero, Vector2.zero, visualizerStyle.scoreTrackColor);
            scoreFill = Panel("Star score fill", scoreTrack, new Vector2(0, .5f), Vector2.zero, Vector2.zero, visualizerStyle.scoreFillColor);
            scoreFill.pivot = new Vector2(0, .5f);
            scoreLabel = Label("Score", strip, new Vector2(.5f, 1), Vector2.zero, new Vector2(80, 24), "0", 18);
            scoreLabel.alignment = TextAlignmentOptions.Right;
            for (int i = 0; i < 3; i++)
            {
                var star = Panel("Score star " + (i + 1), strip, new Vector2(.5f, 1), Vector2.zero, Vector2.one * visualizerStyle.starSize, Color.white)
                    .GetComponent<Image>();
                star.sprite = visualizerStyle.emptyStar; star.preserveAspect = true;
                scoreStars.Add(star); scoreStarsLit.Add(false);
            }
        }

        float[] StarScores => new[] { (float)preparedWorld.rules.StarScore(1), (float)preparedWorld.rules.StarScore(2), (float)preparedWorld.rules.StarScore(3) };
        float ScoreRange => Mathf.Max(1, (float)preparedWorld.rules.StarScore(3) * visualizerStyle.scoreRangePastThreeStars);

        void LayoutVisualizerOverlays(float available)
        {
            guideLeft = -available * .5f; guideRight = available * .5f;
            float stripMiddle = -visualizerStyle.scoreStripHeight * .5f - 8;
            float trackLeft = guideLeft, trackRight = guideRight - 90;
            scoreTrack.anchoredPosition = new Vector2((trackLeft + trackRight) * .5f, stripMiddle);
            scoreTrack.sizeDelta = new Vector2(trackRight - trackLeft, 6);
            scoreLabel.rectTransform.anchoredPosition = new Vector2(guideRight - 40, stripMiddle);
            var scores = StarScores;
            for (int i = 0; i < scoreStars.Count; i++)
                ((RectTransform)scoreStars[i].transform).anchoredPosition =
                    new Vector2(trackLeft + (trackRight - trackLeft) * Mathf.Clamp01(scores[i] / ScoreRange), stripMiddle);
        }

        void UpdateVisualizerOverlays(float scale)
        {
            // Threshold guides share the bars' eased scale, so a bar crossing a line really meets that share.
            double shownTotal = 0;
            foreach (var element in displayedElements) shownTotal += shown[(int)element];
            float matchValue = (float)(shownTotal * composition.RecipePercent / 100);
            float selfValue = (float)(shownTotal * composition.SelfPercent / 100);
            // Worlds without recipe discoveries show no recipe guides.
            foreach (var guide in new Component[] { matchZone, selfZone, matchLine, selfLine, matchLineLabel, selfLineLabel })
                if (guide.gameObject.activeSelf != RecipesEnabled) guide.gameObject.SetActive(RecipesEnabled);
            bool empty = visualizerScale <= 0;
            float top = barBaseline + maximumBarHeight;
            float matchY = empty ? top : Mathf.Min(top, barBaseline + matchValue / scale * maximumBarHeight);
            float selfY = empty ? top : Mathf.Min(top, barBaseline + selfValue / scale * maximumBarHeight);
            float width = guideRight - guideLeft, thickness = visualizerStyle.guideLineThickness;
            Place(matchZone, matchY, selfY - matchY); Place(selfZone, selfY, top - selfY);
            Place(matchLine, matchY - thickness * .5f, thickness); Place(selfLine, selfY - thickness * .5f, thickness);
            void Place(RectTransform rect, float y, float height)
            {
                rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(width, Mathf.Max(0, height));
            }
            bool matchPinned = empty || matchY >= top - .5f, selfPinned = empty || selfY >= top - .5f;
            matchLineLabel.text = visualizerStyle.matchLabel + (matchPinned ? pinnedMark : "");
            selfLineLabel.text = visualizerStyle.selfLabel + (selfPinned ? pinnedMark : "");
            // When both lines pin to the top, the labels sit side by side instead of on top of each other.
            matchLineLabel.rectTransform.anchoredPosition = new Vector2(guideRight - 4 - (selfPinned && matchPinned ? 70 : 0), matchY + 2);
            selfLineLabel.rectTransform.anchoredPosition = new Vector2(guideRight - 4, selfY + 2);
            selfLineLabel.gameObject.SetActive(RecipesEnabled && (!Mathf.Approximately(matchY, selfY) || selfPinned));

            // Star score strip: eased fill, stars switch to the rating star as their thresholds are passed.
            // Uses the same score the world rating is earned from (LevelScore: elemental total plus bonuses, which
            // completion saves as highestScore), not the raw per-sphere points.
            float ratingScore = (float)RatingScore;
            shownScore = Mathf.Lerp(shownScore, ratingScore, 1 - Mathf.Exp(-Time.deltaTime * 6));
            if (Mathf.Abs(shownScore - ratingScore) < .5f) shownScore = ratingScore;
            scoreFill.sizeDelta = new Vector2(scoreTrack.sizeDelta.x * Mathf.Clamp01(shownScore / ScoreRange), scoreTrack.sizeDelta.y);
            scoreLabel.text = System.Math.Truncate(RatingScore).ToString("N0");
            var scores = StarScores;
            for (int i = 0; i < scoreStars.Count; i++)
            {
                bool lit = shownScore >= scores[i];
                if (lit == scoreStarsLit[i]) continue;
                scoreStarsLit[i] = lit;
                scoreStars[i].sprite = lit ? WorldCatalog.Current.ratingStar : visualizerStyle.emptyStar;
                // Silent pop: the star-score sound stays reserved for the end-of-level rating.
                if (lit) StartCoroutine(PopStar(scoreStars[i].transform));
            }
        }

        IEnumerator PopStar(Transform star)
        {
            for (float t = 0; t < .35f; t += Time.deltaTime)
            {
                // The visualizer can rebuild mid-pop (e.g. the bar count changes); stop if this star is gone.
                if (!star) yield break;
                float p = t / .35f;
                star.localScale = Vector3.one * (1 + .6f * Mathf.Sin(p * Mathf.PI) * (1 - p * .4f));
                yield return null;
            }
            if (star) star.localScale = Vector3.one;
        }
    }
}
