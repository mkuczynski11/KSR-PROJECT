<template>
  <div class="home">
    <b-container>
      <b-row> 
        <b-col cols="2"></b-col>
        <b-col cols="8">
          <b-card>
            <h4>Welcome to our Library!</h4>
            <hr/>
            <h5>In the <i>Książki</i> tab You may find list of all the books available for purchase.</h5>
            <h5><i>Zamówienia</i> tab contains all the orders placed in the previous tab, with some most important info about them.</h5>
            <h5>In the last tab <i>Faktury</i> You may find invoices for only the confirmed orders, regardless of whether they were paid or not.</h5>
          </b-card>
        </b-col>
        <b-col cols="2"></b-col>
      </b-row>
    </b-container>
  </div>
</template>

<script>
export default {
  name: "Home",
};
</script>
