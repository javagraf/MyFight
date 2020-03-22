   static void Main(string[] args)
        {




            bool isAnyWin = false;
            bool firstPlayerTurn = false;
            bool secondPlayerTurn = false;
            //SetTurns(ref firstPlayerTurn, ref secondPlayerTurn);

            Random random = new Random();
            //int rNumber = random.Next(1, 100);
           

          
            for (int i = 0; i < 5000; i++)
            {
                int firstPlayerI = 0;
                int secondPlayerI = 0;

                firstPlayerTurn = true;

                List<Card> myCards = new List<Card>()
            {
                new Card{Attack = 8, Health = 8, Name = "Король-лич"},
                new Card{Attack = 1, Health = 1, Name = "Милый мурлок"},
                new Card{Attack = 1, Health = 1, Name = "Защитница Авангарда"},
                new Card{Attack = 3, Health = 3, Name = "Миротворец Алдора"},
                new Card{Attack = 4, Health = 12, Name = "Изера"},
                new Card{Attack = 12, Health = 12, Name = "Смертокрыл"},
                new Card{Attack = 1, Health = 1, Name = "Кабанчик"},
            };
                List<Card> myFriendCards = new List<Card>()
            {
                new Card{Attack = 8, Health = 8, Name = "Король-лич"},
                new Card{Attack = 1, Health = 1, Name = "Милый мурлок"},
                new Card{Attack = 1, Health = 1, Name = "Защитница Авангарда"},
                new Card{Attack = 3, Health = 3, Name = "Миротворец Алдора"},
                new Card{Attack = 4, Health = 12, Name = "Изера"},
                new Card{Attack = 12, Health = 12, Name = "Смертокрыл"},
                new Card{Attack = 1, Health = 1, Name = "Кабанчик"},
            };
                while (true)
                {
                    if (myCards.Any() && myFriendCards.Any())
                    {
                        if (firstPlayerTurn)
                        {
                            if (firstPlayerI < 0)
                            {
                                firstPlayerI = 0;
                            }
                            if (secondPlayerI < 0)
                            {
                                secondPlayerI = 0;
                            }
                            Card myCard = myCards[firstPlayerI];
                            Card randomEnemyCard = myFriendCards[random.Next(0, myFriendCards.Count() - 1)];
                            AttackCard(myCard, randomEnemyCard, true);
                            firstPlayerI++;
                            if (myCard.Health <= 0)
                            {
                                myCards.Remove(myCard);
                                firstPlayerI--;
                            }
                            if (myCards.Count == 1)
                            {
                                firstPlayerI = -1;
                            }

                            if (randomEnemyCard.Health <= 0)
                            {
                                myFriendCards.Remove(randomEnemyCard);
                                secondPlayerI--;
                            }
                            if (myFriendCards.Count == 1)
                            {
                                secondPlayerI = -1;
                            }

                            firstPlayerTurn = false;
                            secondPlayerTurn = true;
                        }
                        else if (secondPlayerTurn)
                        {
                            if (firstPlayerI < 0)
                            {
                                firstPlayerI = 0;
                            }
                            if (secondPlayerI < 0)
                            {
                                secondPlayerI = 0;
                            }
                            Card enemyCard = myFriendCards[secondPlayerI];
                            Card randomMyCard = myCards[random.Next(0, myCards.Count() - 1)];
                            AttackCard(enemyCard, randomMyCard, false);
                            secondPlayerI++;

                            if (enemyCard.Health <= 0)
                            {
                                myFriendCards.Remove(enemyCard);
                                secondPlayerI--;
                            }
                            if (myFriendCards.Count == 1)
                            {
                                secondPlayerI = -1;
                            }

                            if (randomMyCard.Health <= 0)
                            {
                                myCards.Remove(randomMyCard);
                                firstPlayerI--;
                            }
                            if (myCards.Count == 1)
                            {
                                firstPlayerI = -1;
                            }
                            firstPlayerTurn = true;
                            secondPlayerTurn = false;
                        }
                    }
                    else
                    {
                        if (myCards.Count() == 0 && myFriendCards.Count() == 0)
                        {
                            Console.WriteLine("Ничья !");

                            break;
                        }
                        if (myCards.Count() == 0)
                        {
                            Console.WriteLine("Друг выиграл !");

                            break;
                        }
                        else if (myFriendCards.Count() == 0)
                        {
                            Console.WriteLine("Я выиграл !");
                            break;
                        }
                    }

                    //for (int i = 0; i < 100; i++)
                    //{

                    //    if (firstPlayerTurn)
                    //    {
                    //        if (myCards.Any() && myFriendCards.Any())
                    //        {
                    //            AttackCard(myCards[i], myFriendCards[random.Next(0, myFriendCards.Count() - 1)]);
                    //        }
                    //        else
                    //        {
                    //            if (myCards.Count() == 0)
                    //            {
                    //                Console.WriteLine("Я выиграл !");
                    //                break;
                    //            }
                    //            else if (myFriendCards.Count() == 0)
                    //            {
                    //                Console.WriteLine("Друг выиграл !");
                    //                break;
                    //            }
                    //        }
                    //        if (myCards[i].Health <= 0)
                    //        {

                    //            myCards.Remove(myCards[i]);
                    //            i--;
                    //        }
                    //        if (myFriendCards[i].Health <= 0)
                    //        {

                    //            myFriendCards.Remove(myFriendCards[i]);
                    //            i--;
                    //        }

                    //        //firstPlayerTurn = false;
                    //    }

                    //}
                }
                //
            }
            Console.WriteLine("Hello World!");
        }
