﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BluffinMuffin.HandEvaluator.Attributes;
using BluffinMuffin.HandEvaluator.Enums;
using BluffinMuffin.HandEvaluator.EvaluatorFactories;

namespace BluffinMuffin.HandEvaluator
{
    public static class HandEvaluators
    {
        private static Dictionary<CardSelectionEnum, AbstractCardsSelector> m_Selectors;
        
        public static HandEvaluationResult Evaluate(IEnumerable<string> playerCards, IEnumerable<string> communityCards, EvaluationParams parms = null)
        {
            var myParms = parms ?? new EvaluationParams();
            if (m_Selectors == null)
            {
                m_Selectors = new Dictionary<CardSelectionEnum, AbstractCardsSelector>();
                foreach (Type t in typeof(AbstractCardsSelector).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(AbstractCardsSelector)) && !x.IsAbstract))
                {
                    var att = t.GetCustomAttribute<CardSelectionAttribute>();
                    if (att != null && !m_Selectors.ContainsKey(att.Selector))
                        m_Selectors.Add(att.Selector, (AbstractCardsSelector)Activator.CreateInstance(t));
                }
            }
            return !m_Selectors.ContainsKey(myParms.CardSelection) ? null : m_Selectors[myParms.CardSelection].SelectCards(playerCards, communityCards, myParms).Select(cards => new BasicEvaluatorFactory().Evaluators.Select(x => x.Evaluation(cards.ToArray())).Where(x => x != null).Max()).Max();
        }

        public static IEnumerable<IGrouping<int, EvaluatedCardHolder>> Evaluate(IStringCardsHolder[] cardHolders, EvaluationParams parms = null)
        {
            var holders = cardHolders.Select(x => new EvaluatedCardHolder(x, parms));
            var orderedHolders = holders.OrderByDescending(p => p.Evaluation).ToArray();
            var currentRank = 0;
            EvaluatedCardHolder lastHolder = null;
            foreach (var h in orderedHolders.Where(h => h.Evaluation != null))
            {
                if (lastHolder != null && h.Evaluation.CompareTo(lastHolder.Evaluation) == 0)
                    h.Rank = lastHolder.Rank;
                else
                    h.Rank = ++currentRank;
                lastHolder = h;
            }
            return orderedHolders.GroupBy(x => x.Rank);
        }
    }
}
