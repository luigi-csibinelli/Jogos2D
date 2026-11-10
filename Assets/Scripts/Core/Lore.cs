namespace Sanatorio
{
    /// <summary>
    /// A história do jogo, contada por fichas espalhadas pelo sanatório.
    /// Texto original. A ordem das fichas segue a ordem natural de exploração
    /// (do spawn até o gerador), mas cada uma faz sentido sozinha.
    /// </summary>
    public static class Lore
    {
        public const string Title = "SANATÓRIO";
        public const string Subtitle = "UMA HOMENAGEM 8-BIT";

        public const string Intro =
            "ABRIL DE 1945. NORTE DA ITÁLIA.\n" +
            "O CABO RAMOS, DA FORÇA EXPEDICIONÁRIA BRASILEIRA,\n" +
            "SE PERDE DA PATRULHA NUMA TEMPESTADE E SE ABRIGA\n" +
            "NO VELHO SANATÓRIO DE VALGRIGIA.\n" +
            "OS PACIENTES AINDA ESTÃO LÁ. NÃO ESTÃO MAIS VIVOS.\n" +
            "SOBREVIVA. ENCONTRE A ENERGIA. DESCUBRA O QUE HOUVE.";

        public static readonly string[] NoteTitles =
        {
            "FICHA 01 - ADMISSÃO",
            "FICHA 02 - DIÁRIO DA ENFERMEIRA",
            "FICHA 03 - ORDEM DE SERVIÇO",
            "FICHA 04 - RELATÓRIO DO DR. ORSINI",
            "FICHA 05 - CARTA NÃO ENVIADA",
            "FICHA 06 - INVENTÁRIO DA COZINHA",
            "FICHA 07 - REGISTRO DO NECROTÉRIO",
            "FICHA 08 - ÚLTIMA ANOTAÇÃO",
        };

        public static readonly string[] Notes =
        {
            "PACIENTE 114. INSÔNIA, FEBRE ALTA.\n" +
            "TRANSFERIDO PARA A ALA OESTE POR ORDEM\n" +
            "DA SOCIEDADE LÁZARO. FAMÍLIA NÃO AVISADA.\n" +
            "TRATAMENTO: SORO L, DOSE 3.",

            "ELES NÃO DORMEM MAIS. PASSAM A NOITE\n" +
            "ARRANHANDO AS JANELAS. O DOUTOR MANDOU\n" +
            "PREGAR TÁBUAS. DISSE QUE É PARA PROTEGER\n" +
            "OS PACIENTES. NÃO SEI MAIS DE QUEM.",

            "INSTALAR GRADES ELÉTRICAS NAS DUAS VARANDAS.\n" +
            "LIGAR TUDO AO GERADOR DO ANDAR DE CIMA.\n" +
            "A PORTA ENTRE AS ALAS SÓ ABRE COM ENERGIA.\n" +
            "ASS.: MANUTENÇÃO.",

            "O SORO L NÃO CURA A MORTE. ELE A ADIA,\n" +
            "E O QUE VOLTA NÃO LEMBRA O PRÓPRIO NOME.\n" +
            "A SOCIEDADE QUER RESULTADOS ATÉ MAIO.\n" +
            "AUMENTAR A DOSE.",

            "QUERIDA MÃE, AQUI NÃO É UM HOSPITAL.\n" +
            "OS REFRIGERANTES QUE NOS DÃO TÊM GOSTO\n" +
            "DE FERRO E NOS DEIXAM FORTES DEMAIS.\n" +
            "SE ESTA CARTA CHEGAR, NÃO VENHA.",

            "FERRO-NOG: 40 CAIXAS. TURBO-COLA: 25.\n" +
            "GATILHO DUPLO: 30. REVIVE-JÁ: ACABOU.\n" +
            "OBS.: AS MÁQUINAS SÓ FUNCIONAM COM O\n" +
            "GERADOR LIGADO. NÃO DESLIGAR À NOITE.",

            "CORPOS RECEBIDOS ESTA SEMANA: 31.\n" +
            "CORPOS NAS GAVETAS PELA MANHÃ: 9.\n" +
            "TRANCAR O NECROTÉRIO POR FORA.\n" +
            "NINGUÉM DESCE SOZINHO.",

            "SE ALGUÉM LER ISTO: A CAIXA DO GERADOR\n" +
            "NÃO É NOSSA. ELA SE MUDA SOZINHA E\n" +
            "DEVOLVE ARMAS QUE NUNCA COMPRAMOS.\n" +
            "O URSINHO AVISA QUANDO ELA VAI EMBORA.",
        };

        public const string Epilogue =
            "VOCÊ JUNTOU TODAS AS FICHAS.\n" +
            "A SOCIEDADE LÁZARO ABANDONOU VALGRIGIA\n" +
            "NA PRIMAVERA DE 1945, LEVANDO O SORO L.\n" +
            "O SANATÓRIO FOI ESQUECIDO. OS PACIENTES, NÃO:\n" +
            "ELES CONTINUAM ESPERANDO NAS JANELAS.";

        public const string Credits =
            "UM JOGO DO GRUPO - INSPER JOGOS DIGITAIS 2026\n" +
            "INSPIRADO EM VERRÜCKT (TREYARCH, 2009)\n" +
            "ARTE, SONS, MÚSICA E CÓDIGO ORIGINAIS\n" +
            "FEITOS EM UNITY 6";
    }
}
